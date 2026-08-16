# Channel 0.2 design-foundation closure review 14 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-14-2026-08-16-6cddb99`

Reviewed commit: `6cddb990f1f8aada3018a19c63b43116b83f05e6`

Date: 2026-08-16

Overall verdict: **`does-not-conform`** — one blocking finding (**AJ1**) and six nonblocking findings
(**AJ2**-**AJ7**).

**AJ1 is AI1 surviving the commit written to close it, on the two artifacts that outrank the three it
was closed in.** The AI1 correction added the session to three published settling-frame field lists —
the neutral brief's local-observation schema, the brief's parity profile, and the interaction state
machine's latch section — and the commit message, the disposition history, and the review policy all
say "all three field lists now carry the session". There are five. The state/event coverage grid
publishes the same reference as `kind, interaction identity, committing endpoint, arrival ordinal`,
and the responsibility matrix's `Local observation content and provenance` row — the **declared owner**
of the observation record — carries the settling frame "with its arrival ordinal" and no session. The
brief states that it is subordinate to the grid; the matrix's own status block states the invariant the
correction broke ("the fact this matrix owns and the fact the parity profile compares are the same
fact") and then declares itself "Unchanged by every correction pass through **AI9**". Probe **P3**
reproduces AI1's exact false green on `C4-outcome-precedes-ack` from both of those two field lists,
unchanged in mechanism from the row review 13 ran.

Every retained finding B1 through AH6 is closed in the artifact it was raised against. **AI1, AI3, AI5,
AI6, AI8, and AI9 are closed.** **AI2 is closed in neither of the two artifacts its evidence names,
and is claimed closed in a third that was never its subject** (**AJ2**). **AI7 is closed in the parity
profile and open in the vector-format bullet its evidence names first** (**AJ3**). **AI4 is closed as
to the family token and open as to both of the two specific staleness sentences its evidence quoted**
(**AJ4**).

## Isolation

Complete, with the dispatch provenance disclosed in its own section below.

```text
C:/b034  ->  6cddb990f1f8aada3018a19c63b43116b83f05e6  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | wc -l     ->  891
git diff HEAD            ->  (empty)
```

The clone materialised completely — 891 tracked paths, clean status, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was read
from `C:/b034`; all four gates available to this review were run there. **The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at any
point in this session.**

The reviewer identity above differs from all thirteen retained reviewers, from every correction author,
and from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md`
and `docs/future/channel/reviews/README.md` were both read from the clone at the pin and are the source
of this review's scope. The `C4-P2` evaluator used in probe **P3** imports no repository code; it was
written from the published prose of C4, the brief's operator set, the brief's vector format, the
brief's parity profile, and the latch sections of the interaction machine and the grid, and it lives
outside the clone.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect and no area of
suspicion. Four things in it narrowed where effort went, and I record them so the next cycle can
discount accordingly.

1. It told me to verify the pin myself rather than take it from the brief, and pointed at U6, X6, and
   AI8 as the reason. I did (see **Pin**); it holds in the tree-hash form and, this time, as to date.
2. It restated the policy's requirement of at least one genuine attempt to falsify a capability-wide
   property. Roughly half the effort here went to C4, C12, the neutral brief, the interaction machine,
   the grid, the matrix, and the four entry-point narratives. C1, C3, C5, C6, C7, C9, and C11 were
   assessed by reading and cross-tracing, with one falsification attempt at `C2-P1`, one at `C8-P1`,
   and one at `C11-P1`.
3. It told me the thirteenth review had found a retained finding open for six cycles behind indexes
   that reported it closed, and asked me to verify closure against the findings' own evidence. I did,
   and three of this attestation's seven findings (**AJ2**, **AJ3**, **AJ4**) come from that one
   instruction. They are re-derivations of AI2's, AI7's, and AI4's own evidence sentences and I claim
   no independent discovery of the *class*; I claim only that I opened the sentences.
4. It told me to read closure review 13's attestation for form. That attestation is a detailed account
   of AI1-AI9, so my verification that the AI corrections landed is verification of findings I had been
   told about. **AJ1's location, AJ5, and AJ6 are in no retained record.** AJ1's *subject* is AI1's
   subject; what is new is that AI1 is not closed and that the check written for it is structurally
   unable to see where it is open.

I did **not** read any retained attestation before forming my own reading of C4, both machines, the
grid, the matrix, and the brief, and before writing and running the `C4-P2` evaluator. Review 13's
attestation was read after the evaluator had already reported a green on the grid's field list; reviews
7-12 were consulted only for specific findings' evidence sections when re-deriving them.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that, on its own account:

- **authored the correction commit under review**, `fix(channel): close AI1-AI9 and sweep by concept`,
  and the follow-up commit `docs(channel): correct the pin clause date to the target commit's date`,
  including every artifact edit and every verifier check in both;
- **also authored the five commits before them** — the AH, AG, AF, AE, and AD corrections — and the
  [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md) retained in this
  directory;
- **dispatched closure reviews 9 through 13**, whose findings those commits correct; and
- **recommended the 2026-08-15 closure-standard ruling**, which the repository owner made on that
  recommendation *after* the twelfth review's verdict was known.

This is the same relationship the directory discloses for closure reviews 10 through 13 — the
dispatcher is the author of the very commit being judged — extended by one further correction commit,
one further dispatch, and the pin-clause commit. It is recorded because an undisclosed relationship
between a dispatcher and a reviewer is the same class of defect as an undisclosed
reviewer-repairs-own-finding, which this directory already discloses twice.

**What the dispatch did and did not carry.** The brief conveyed none of the dispatching session's
findings, reasoning, or conclusions. It named no artifact defect, no area of suspicion, and nothing
about where it believed the work was weak or strong; my context contains nothing from that session
beyond the brief itself. It pointed me at `AGENTS.md` and this directory's policy, told me to take my
scope from them rather than from the brief, told me explicitly that I was reviewing work whose author
had arranged my review and that this was a reason to probe the corrections harder rather than defer to
them, told me that twelve reviews returned `does-not-conform` and one returned
`conforms-with-nonblocking-findings` and that this was context rather than a target in either
direction, and stated that neither manufacturing a finding to avoid committing to a verdict nor
suppressing one to reach a cleaner verdict was acceptable. It told me the 2026-08-15 ruling exists and
told me to read it myself rather than take its summary, which I did.

**Did anything in the dispatch narrow where I looked?** Yes, as recorded in the caveat above: the
instruction to verify the pin, the instruction to run an evaluator, the instruction to check retained
findings against their own evidence, and the instruction to read review 13 for form concentrated effort
on the pin clause, on C4/C12/the brief/the machines, and on the AI findings. **Nothing in the dispatch
narrowed what I concluded.** The blocking finding is inside the dispatching author's own change, on
the two artifacts that change's own check cannot read, and the dispatching brief gave me no reason to
prefer the grid and the matrix over the three artifacts the correction did edit — I reached them by
asking the question the commit message itself declares the new method to be, and getting a different
answer than the commit did.

**One note on the arrangement.** The standard against which my verdict is scored was recommended by the
dispatching session, after a favourable verdict arrived, and under it any finding at all withholds
closure. That removes any incentive to soften a finding to reach a clean verdict, since a clean verdict
is unreachable with even one remark. It does not bear on whether **AJ1** is real; that rests on probe
**P3**, which anyone can re-run from the published prose.

## Pin

The policy's pin clause names the current target as the commit titled
`fix(channel): close AI1-AI9 and sweep by concept`, "or any later commit whose design artifacts hash
identically to it — and check that claim rather than assuming it, because this clause has now gone
stale twice" (U6, then X6, then AI8 as to date).

I checked it against the repository rather than against the brief, and it holds in both the subject and
the date form, and in the stronger design-artifact form:

```text
git log -1 --format=%s d0fa776        ->  fix(channel): close AI1-AI9 and sweep by concept
git log -1 --format=%ad --date=short  ->  2026-08-16   (clause says "committed 2026-08-16")
git diff --stat d0fa776 6cddb99       ->  docs/future/channel/reviews/README.md | 2 +-
git diff --stat 129122c 6cddb99       ->  (empty)
```

The only difference between the named commit and the reviewed merge is one line of
`docs/future/channel/reviews/README.md`, which is not in the design-artifact pathspec: it is
`129122c`, correcting the pin clause's own date from 2026-08-15 to 2026-08-16 because the correction
commit landed after midnight. **AI8 is therefore closed, and closed by its own check firing on its own
first run** — the commit message for `129122c` says so and the history bears it out. Every design
artifact at `6cddb99` hashes identically to `d0fa776`. `6cddb99` is the merge of PR #124; `d0fa776`
carries exactly the named subject and is the head of the correction sequence beginning at
`fix(channel): make C4-P2 falsifiable`.

## Blocking findings

### AJ1 — AI1 is closed in three of the five published settling-frame field lists; the two left are the grid the brief is subordinate to and the matrix that owns the fact, and `C4-P2` evaluates green on `C4-outcome-precedes-ack` from both

**Artifacts.**
`Brontide-Channel-0.2-State-Event-Coverage-0.1.md` §"Late-traffic latch", lines 134-139, and its
`Status:` block, line 18;
`Brontide-Channel-0.2-Responsibility-Matrix-0.1.md` §ownership table, the
`Local observation content and provenance` row, line 107, and its `Status:` block, lines 5-18;
`Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` §new-evidence inventory, line 286;
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Local observation" lines 174-177,
§"Observation and parity profile" lines 340-350, and the subordination sentence at lines 31-34;
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §latch, lines 215-216;
`build/verify-channel-0.2-design.ps1`, the AI1 block at lines 1601-1628 and the AC1 blocks at lines
540-563;
`docs/future/channel/README.md`, the State/event coverage and Responsibility matrix rows, lines 52-53.

**What the correction did.** AI1's evidence was that the settling-frame reference is published as four
fields with no session while a vector may now legitimately carry two sessions holding one interaction
identity value, so the reference stops mapping to one declared stimulus step and `C4-P2` goes green on
its own named mutation. The commit added `its **session**` to three field lists, and says so in four
places:

> The settling-frame reference … stayed published **in three places** as four fields with no session
> … **All three field lists now carry the session.** *(commit message; the same sentence appears in the
> completeness review's disposition history at lines 598-602 and in the review policy's step 3o)*

> the settling-frame check reads **every published field list**, rather than the ones a finding named
> *(commit message, "THE SWEEP AXIS CHANGED")*

**There are five, and the two it did not reach are the two that outrank the three it did.**

The state/event coverage grid, unmodified at this pin except for one appended status sentence:

> Settling the latch records the frame that settled it — kind, interaction identity, committing
> endpoint, and its **arrival ordinal** within the interaction — because the three latch values name no
> frame and `C4-P2`'s second conjunct is about which frame a latch settled against. *(lines 134-136)*

The responsibility matrix's owner row, unmodified except for one appended status sentence:

> C10 local observation record, including the late-traffic latch with its `not-applicable` value, **the
> frame that settled it with its arrival ordinal**, and the kind and provenance of a refused frame that
> opens no interaction *(line 107, crossing-artifact column)*

And the migration ledger's inventory of what Batch 2 must build, which lists the same reference the
same way (line 286).

**Why these two are not subordinate surfaces.** The neutral brief says so itself: "The brief is
subordinate to the C1-C12 capability contract, both state machines, and the closed state/event coverage
grid. If a convenient schema shape contradicts them, the schema changes" (lines 31-34). Two of the
three artifacts now carrying the session are the brief; the third is the machine. The grid is the
fourth authority in that sentence and it contradicts them. This is not a novel reading — it is
**AC1's**, and AC1 is retained in this directory with exactly this reasoning: "Y4's arrival ordinal was
stated in the neutral brief and nowhere else, and the brief is subordinate to the contract, both state
machines, and the grid, so the hierarchy resolved the contradiction against the fix: the interaction
machine that owns the latch, **the grid that enumerates the cells asserting it, and the matrix row AB2
had just added** all still named X1's three fields."

The matrix is stronger still, because it is not a secondary statement of the fact but the **owner** of
it, and its own status block states the invariant this commit broke:

> Under AC1 that row's crossing artifact carries the settling frame's arrival ordinal and the refused
> frame's kind, **so the fact this matrix owns and the fact the parity profile compares are the same
> fact**. *(matrix status block, lines 7-9)*

At this pin the parity profile compares a session and the crossing artifact does not carry one, so that
sentence is false. The verifier's own AC1 message states the rule being violated in the general form:
"The crossing artifact must carry every field the parity profile compares, **or the owned fact and the
compared fact are different facts**" (line 563).

**Probed, not reasoned.** Probe **P3** runs one evaluator with the settling-frame field list as the
only variable, transcribed verbatim from each of the five artifacts that publish it. Vector
`M2-two-session` is review 13's: session B wholly conforming and reusing interaction identity `x1`,
session A carrying the named mutation `C4-outcome-precedes-ack`. Design expects **red**.

| settling-frame reference as published by | M2 single-session | M2-two-session | required-green 6 | required-green 7 |
| --- | --- | --- | --- | --- |
| brief, Local observation (174-176) — 5 fields | red | red | green | green |
| brief, parity profile (340-342) — 5 fields | red | red | green | green |
| interaction machine, latch (215-216) — 5 fields | red | red | green | green |
| **state/event grid, Late-traffic latch (134-135) — 4 fields** | red | ***green*** | green | green |
| **responsibility matrix, owner row (107) — 4 fields** | red | ***green*** | green | green |

The mechanism is unchanged from AI1: the four published fields match a declared step in each session,
the reference no longer maps to one, an evaluator binds it to the conforming session's
acknowledgement, that session has no terminal frame for the identity, and precedence — correctly
restricted to one session under AG2 — returns no verdict, so the real violation in the other session
goes unwitnessed.

**The check written for AI1 cannot see it, by construction.** Lines 1610-1628 iterate exactly two
artifacts:

```powershell
foreach ($settlingArtifact in @(@{ Name = 'neutral brief'; Text = $neutralBrief },
                                @{ Name = 'interaction state machine'; Text = $interaction })) {
    foreach ($settlingMatch in [regex]::Matches((Get-FlowedText $settlingArtifact.Text),
                                                '(?:its|frame''s) kind, its(.{0,95})')) { … }
}
if ($settlingFrameLists.Count -lt 3) { … }
```

I ran that regex myself over all four candidate artifacts (probe **P5**). It matches 2 lists in the
brief and 1 in the machine, and **0 in the grid and 0 in the matrix** — both because they are not in
the iteration list and because neither phrases the list as "its kind, its …". The guard
`Count -lt 3` is set to exactly the number of lists in the two artifacts it reads, so the check
certifies its own completeness against its own scope. The same script already carries **four** AC1
checks for the arrival ordinal, at lines 540, 554, 558, and 562, covering the brief, the machine, the
grid, and the matrix — the correct artifact set for this exact reference was already enumerated in this
file, thirty lines from where the two-artifact loop was written.

**And both indexes assert the opposite.** The grid's status block now reads "Unchanged by every
correction pass through **AI9**" (line 18), the matrix's the same (line 18), and the Channel index rows
for both read "unchanged by AI9" (lines 52-53). Those are affirmative claims that the AI family did not
touch these artifacts, made in the commit that changed the fact they publish.

**Why I rate this blocking, and the exact condition under which an owner should not.**

Blocking, on four grounds. It is a demonstrated false green on a named mutation, which this programme
has ruled blocking every time it has appeared — U1, AC3, AF1, AG1, AI1 — and which C12 makes a finding
against the property outright. It is *inside the reviewed commit's own subject*: the finding is not
that AI1 was mis-analysed but that the correction reached three of five surfaces while asserting five,
under a method the same commit introduces and names. The unreached surfaces are the ones the design's
own hierarchy resolves in favour of, and one of them is the declared owner of the fact, whose status
block states the invariant as an accomplished one. And the trigger is nameable, concrete, and already
executed.

The counter-reading, stated so the disagreement is locatable. The grid's Purpose says "The
state-machine transition tables remain the detailed authority. This grid proves their event domain is
closed", so an owner could hold that the grid's latch paragraph is derivative of the machine's and
inherits its correction, and that the matrix row's crossing-artifact column is a pointer rather than a
schema. **If the owner holds that, AJ1 is nonblocking and my verdict is wrong.** I do not hold it, for
three reasons: AC1 was raised and corrected on precisely the opposite reading, with a dedicated check
for each of the four artifacts; the brief's subordination sentence names the grid explicitly and there
is no comparable sentence subordinating the grid to the machine on observation content; and the matrix
row is not a pointer but the crossing artifact the verifier's own message says must carry every
compared field.

Two things are **not** wrong, and I record them because a correction pass should not widen the fix.
The three corrected field lists are correct and complete — probe **P3** confirms them red on both named
mutations and green on all seven required-green members. And AI1's analysis was right: the ambiguity is
real, the session is the right field, and the correction's placement in the brief and the machine is
where it belongs. AJ1 is the two artifacts it stopped short of.

## Nonblocking findings

### AJ2 — AI2 is closed in neither of the two artifacts its evidence names, and is claimed closed in a third that was never its subject

**Artifacts.** `docs/future/README.md` §"Priority 1 — Channel 0.2 redesign and migration", lines 32-70;
`docs/future/channel/README.md` §"Channel 0.2 design foundation", the opening narrative lines 6-24 and
the second narrative lines 28-44; `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` status
block, line 35; `docs/future/channel/README.md`, the plan row, line 48;
`build/verify-channel-0.2-design.ps1`, the AH3 and AA2 checks.

AI2's evidence named two documents. Both were changed, and in both the change is the same token
substitution AI2 was raised for.

**`docs/future/README.md`.** The correction changed `12 retained independent reviews` to `13`,
`No independent review has yet seen the AH corrections` to `AI`, and the table row at line 1645. AI2's
first evidence sentence was that this document "carries the eleventh review's existence nowhere in its
Priority 1 prose … `AG` appears nowhere else in the file". That is still exactly true, and is now true
of the twelfth and thirteenth as well. The narrative runs ninth review → AE, tenth review → AF, "All
eight are corrected", and then jumps to "No independent review has yet seen the AI corrections" — a
family the prose never introduces, and the third consecutive family to be introduced that way.
`grep -n "\bAG[0-9]\|\bAH[0-9]\|\bAI[0-9]"` over the whole file returns exactly **two** hits: line 68
and the table row at line 1645. The words "eleventh", "twelfth", and "thirteenth" do not appear.

**`docs/future/channel/README.md`.** The correction changed the range sentence to "S1 through **AI9**"
and updated all nine artifact rows. AI2's second evidence sentence was that the narrative "narrates
'the ninth closure review raised AE1-AE5 …; the tenth raised AF1-AF8 … Both are corrected', and stops.
No eleventh review, no AG family, and the paragraph below it still ends 'and most recently a fix stated
only in the artifact that reads the fact rather than the ones that own it', which describes AC1." All
three clauses are still true **verbatim** at this pin: lines 13-17 and line 43.

**And the plan now claims the correction.** Its status block gained:

> Under **AI2**, **AI5**, and **AI9** this plan is corrected again: **two narrative surfaces that
> stopped at the tenth review**, … *(line 35)*

The plan's own narrative does not stop at the tenth review — AH3 carried it to the twelfth (lines
24-32) — and the two surfaces that do stop at the tenth review are in the two other documents and are
unchanged. The Channel index's plan row repeats the claim ("AI2, AI5 and AI9 corrected"). So AI2 is
recorded as closed in an artifact it was not raised against, while remaining open in both artifacts it
was. That is AG2's cross-artifact class applied to a finding's own disposition, and it is what makes
this instance worse than the six before it rather than merely the seventh.

**Why no gate sees it.** Unchanged from AI2: the AA2 family check asks only that each family appear
*somewhere* in `docs/future/README.md`, which line 1645 satisfies; the AH3 check reads that file's
"no independent review has yet seen the … corrections" sentence and the plan's status block; the AF2
check reads the Channel index's range sentence alone. Nothing reads a narrative.

Nonblocking, on the programme's unbroken precedent for entry-point staleness (S3, AA1/AA2, AE4, AF2,
AG4/AG5, AH3, AI2). Recorded because this is the eighth consecutive cycle, because review 12 predicted
the seventh in writing and review 13 predicted the eighth in writing — "If the pass updates the counts
and the plan's status block without reading the narratives above them … all three findings survive the
commit that closes them" — and because the false closure claim in the plan is new.

### AJ3 — AI7 is closed in the parity profile and open in the vector-format bullet its evidence names first

**Artifacts.** `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector format", the first bullet
(line 198) and §"Observation and parity profile", the first compared field (lines 320-321);
`build/verify-channel-0.2-design.ps1`, the AI7 check at lines 1647-1652.

AI7's evidence named two entries in two lists. The parity profile's is corrected:

> - the exact established profile digest **of each session the vector carries**, which is AI7 …

The vector format's is not. Line 198 is unchanged from the AH1 commit:

> - profile, and the initial session/interaction state of **each session the vector carries**.

`profile` is still outside the distribution, exactly as AI7 stated it: "`profile` was not distributed
over the sessions the same sentence just made plural." The AI7 check reads
`established profile digest \*\*of each session` and nothing else, so it passes on the corrected half
alone.

This is the seventh instance of the closed-in-the-first-artifact pattern — AE4→AF2, AE5→AF3, AF1→AG1,
AF2→AG4, AF5→AH2, AH5→AI3, now AI7→here — and the second (after AI3) to be a single finding's two named
positions rather than two documents. The commit's own recorded lesson, in the completeness review at
lines 591-593, is "enumerate the artifacts a correction *touches* rather than the artifacts a finding's
author happened to cite"; AI7's author cited both bullets, and the sweep touched one.

Nonblocking on AI7's own rating and for its own reasons: no required vector group names a multi-session
vector, no property is evaluated differently, and the fix is a plural.

### AJ4 — AI4 is closed as to the family token and open as to both of the specific staleness sentences its evidence quoted

**Artifacts.** `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` `Status:` block, line 8;
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` `Status:` block, lines 16-17;
`build/verify-channel-0.2-design.ps1`, the AI4 check at lines 1630-1645.

The new check is a real improvement and it works: it iterates all eleven `$artifactNames` and requires
each status block to reach the newest family, and every block now does. But AI4 was not only a
family-currency finding. Its evidence paragraph named two specific false self-descriptions, and both
are unchanged:

> The brief is behind by four families in the artifact this commit edited twice, and **its status block
> still describes the operand AH1 changed in its pre-AH1 form: "stimulus steps name their committing
> endpoint so that relation has an operand"**. **The completeness review's block still says "the
> disposition history now runs to the eighth cycle", and it runs to the twelfth** — which is U4's own
> defect restored as a self-description, and AD3's class. *(AI4, review 13)*

At this pin the brief's status block still reads "stimulus steps name their committing endpoint so that
relation has an operand" (lines 16-17), with no session, in the same block that now says "Under **AI1**
the settling-frame reference carries its session". And the completeness review's block still reads "The
disposition history now runs to the eighth cycle rather than stopping at the fifth" (line 8), in a
document whose history now runs to the **thirteenth**. Appending a sentence naming the newest family
satisfies the check and leaves both quoted sentences saying what AI4 said they say.

Nonblocking on the same precedent as AJ2. Recorded because it is the countable half of a finding being
fixed while the quoted half is not, which is the shape review 13 named for AI4 itself, and because the
completeness review's block is now U4's defect at two removes: a self-description that understates the
document by five cycles, in the artifact whose disposition history is the package's own record of what
has been fixed.

### AJ5 — the AH4 escape-clause binding is defeated by naming the family's last finding, and the two rows where that matters are the two AJ1 is about

**Artifacts.** `build/verify-channel-0.2-design.ps1`, the AG4/AH4 block at lines 1497-1515;
`docs/future/channel/README.md`, the artifact rows at lines 50, 52, 53, and 55.

AH4 bound AG4's escape clause to the newest family so a row could not satisfy every future check with
one unbound phrase. The row test is:

```powershell
if ($artifactRow -cnotmatch "\b$($latestDispositionFamily[0])[0-9]" -and
    $artifactRow -cnotmatch "unchanged by[^|]*\b$($latestDispositionFamily[0])\b") { … }
```

Four rows now read "unchanged by **AI9**". `\bAI\b` does not match `AI9` — there is no word boundary
before the digit — so the escape clause does not fire. They pass through the **first** clause,
`\bAI[0-9]`, which `AI9` satisfies. The consequence is that a row can now discharge its obligation by
declaring itself unchanged by one *finding* of the family rather than by the family, which is exactly
what AH4 was written to prevent, reached by a wording AH4's mutation test did not try. Review 13's probe
**P5** tested "unchanged by AF and AG" and "unchanged by AH"; it did not test "unchanged by AH6".

This is not theoretical here. Two of the four rows are State/event coverage and Responsibility matrix,
and both are wrong at this pin for the reason **AJ1** gives: the artifacts are affected by AI1 and were
not updated. "Unchanged by AI9" is a true statement about AI9 and a false impression about the family,
and the check accepts it because it reads the string and not the claim. The same wording appears in
both artifacts' own status blocks in the stronger and plainly false form "Unchanged by every correction
pass through **AI9**".

Nonblocking as a finding against the check rather than against a design fact; it is recorded separately
from AJ1 because fixing AJ1 without fixing this leaves the mechanism that concealed it intact.

### AJ6 — the AI1 insertion breaks the interaction machine's own justification for the arrival ordinal

**Artifacts.** `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §latch, lines 215-219;
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Observation and parity profile", lines 370-372.

The session was inserted as the **second** field of a five-field list whose next sentence counts from
the front:

> Settling the latch also **records the frame that settled it** — its kind, its **session**, its
> interaction identity, the endpoint that committed it, and its **arrival ordinal** within that
> interaction — in the local observation. **The first three do not identify the frame** when one
> endpoint commits two of the same kind for one identity, which is exactly what a duplicate terminal is
> …

Before the insertion "the first three" were kind, interaction identity, and committing endpoint, and
the sentence was Y4's argument that the ordinal is necessary *given the endpoint*. After it the first
three are kind, session, and interaction identity — a set that omits the committing endpoint the claim
is about. The sentence remains literally true and no longer establishes what it exists to establish: a
reader can satisfy it by adding the endpoint instead of the ordinal. The grid's parallel sentence, "The
ordinal is required for the same reason **the other three** are insufficient" (line 136-137), is still
correct precisely because the grid was not edited — the two artifacts now disagree about which fields
the argument is over as well as about how many there are.

The mechanical cause is the one AI6 named. Sixty lines longer than 110 characters were added to
`docs/future/` in this commit, in documents that otherwise wrap near 100, including a 350-character
status line in this artifact, 304 in the brief, 372 in the plan, 241 in the completeness review, and
199 in the contract — each an insertion appended to an existing line rather than folded into the
paragraph. The brief's mid-clause break AI6 also named ("The conjunct\n tests membership of the
identity in the\n set the recipient admits", lines 370-372) is unchanged.

Nonblocking: no property changes verdict, and AI6's substantive half — naming `C4-P2`'s membership
subject instead of leaving it to the nearest antecedent — is correctly done in the contract at lines
295-297. Recorded because it is AI6's own class occurring in the same commit that closes AI6, for the
second consecutive cycle, and because this instance damages an argument rather than only a pronoun.

### AJ7 — the retained-attestations list places review 13 before review 12

**Artifacts.** `docs/future/channel/reviews/README.md` §"Retained attestations", lines 600-620.

The list is chronological from the original attestation through closure review 11, and then reads 13,
12. A reader scanning for the most recent retained record finds the thirteenth in the eleventh's place
and the twelfth after it.

Nonblocking and the lowest-weight finding here; nothing is misstated and both entries are accurate.
Recorded rather than passed over for one reason only: AI8 established that a defect of exactly this
weight, seen and dispositioned as "noted, not raised", is not a disposition this programme's machinery
can act on.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | One immutable profile established before any interaction is dispatchable; negotiated and fixed paths yield the same inspectable facts; unknown Channel versions, required features, classes, authority modes, and incompatible application contracts refuse; no implicit downgrade and no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors — either exactly one profile with every normative fact equal, or nothing dispatchable with `known-none`. The established-profile image carries the realization's per-interaction frame order declaration, and W2's point that establishment verifies the declaration is *present*, never *true*, remains stated at the provider boundary. **AJ3** touches C1's territory from the brief's side — the vector format's `profile` is still singular where a two-session vector has two — and is recorded there; the parity profile's digest is correctly per-session. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain refusing new interactions while admitted ones reach a terminal fact, D1's duplicate drain fatal with the first snapshot preserved and no interaction's effect certainty rewritten. `C2-P1` covers acceptance, the leave-unchanged-or-fault alternative, and terminal monotonicity. A falsification attempt (probe **P6**) failed: no session grid cell routes a terminal state to a nonterminal one, and the `closed`/`faulted` rows carry "terminal late input" or "remains `closed`/`faulted`; local observation only" in all six columns. Interconnection, Ready, Release, withdrawal, and Component termination are each listed as explicitly not session states. C2's Silence disclaims reconnect, which AI5 correctly resolved by withdrawing the citation rather than editing C2. |
| C3 | conforms | Class, direction, and external phase are three separate exact admission inputs evaluated before dispatch; `false` and `unknown` are treated identically; the receiver's independently derived phase gets D3's frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger's `state-violation` row. Channel evaluates the declared predicate without creating or advancing the phase, and `C3-P1` binds all three inputs conjunctively. The Portable Binding 0.2 profile's two declared classes match C7 and Decision 13. |
| C4 | does-not-conform | **AJ1** is against `C4-P2`'s second conjunct as two artifacts publish its operand, and is blocking. What is sound is sound and I record it: probe **P3** finds `C4-P2` green on all seven legal members of its required vector group and red on both named mutations under all three corrected field lists, so AE1, AF1, AF5, AG1, AH1, AH2, and AI1's own analysis all hold. `C4-P1`'s three clauses, the finite positive `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, W4's retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, AF8's session-scoped membership operand, AH6's coverage limit, AI6's named membership subject, and both conjuncts' restriction to one endpoint's own frames all hold. What does not hold is that the settling-frame reference maps to one declared step in every artifact that publishes it. |
| C5 | conforms | Positional payload/authority classification with authority positions never projecting; parsing and structural validation before handler dispatch; no partial or oversized frame becoming a partial interaction; `known-none` on every pre-dispatch structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits tighter than the profile's must be exposed and accepted at establishment, which is where the retained register's `CH-K6` hardening asymmetry is answered. |
| C6 | conforms | Authority evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility each explicitly disclaimed as grants; local denial emitting no frame and recording `known-none`; cross-trust carrying attributable context and exact designations and no Capability, Constraint expression, or derivation chain. `C6-P1` requires exactly one `permitted` local decision to reach dispatch. |
| C7 | conforms | Traced clause by clause against Decision 13 in `binding/portable/open-decisions.md`: Option A retained for 0.1, Option B selected for 0.2, C and D rejected, recorded 2026-08-11 by user:JakHoh. C7 carries Option B's exact CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the post-Interconnection pre-Ready window; the composition root initiating on the Component's behalf; the refusal to introduce a Component-to-Component binding kind; and failure preventing Ready and Release while returning the actual observation to CM4 cleanup or rollback. `C7-P1` forbids the interaction producing Ready or Release by itself. Option B's wording says "a new envelope kind" and C7 uses the ordinary interaction form; that departure is explicit, reasoned in the completeness review, and recorded in the matrix's boundary ruling. |
| C8 | conforms | One accepted terminal history from five named forms; cancellation an optional core control with fixed meaning and exactly one request per nonterminal dispatched interaction; the acknowledgement explicitly nonterminal in both `accepted` and `refused` forms; R1's held control bounded at exactly one with R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating`; T3's `cancelled`-with-no-request-in-force routed as a class at both endpoints. A falsification attempt (probe **P6**) failed: the T3 route commits `internal-channel-failure` at the recipient and a peer fault at the initiator, so neither endpoint records it as semantic success, and the late-traffic latch preserves the first accepted history on every duplicate route in both grids. C8's statement that recipient admission is not observable from `dispatched` is what makes AE1's loss vector legal and is correctly unchanged. |
| C9 | conforms | Four provenance forms with an exclusivity property; an unknown peer-fault category faulting the local session as `unrecognized-peer-fault` with no answering fault and no loop; loss categories and detection points observer-relative and claiming no global topology. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement or a protocol fault as an Outcome. PB8's blocking finding — both stacks fabricating a known zero effect count on process loss — is answered by C10's certainty form rather than restated as a Channel 0.1 defect. |
| C10 | does-not-conform | C10 is the capability that owns the observation record, and **AJ1** is that record's settling-frame position published two ways at once: five fields in the brief's schema and the machine, four in the grid's latch section and in the matrix row C10's own content is owned by. C10 itself is correct and unchanged — AE2's `known-none`, AC2's refused-frame kind and detailed reason, Y1/Y2's latch and settling frame, and Z3's `not-applicable` are all present, and `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. The defect is that the artifacts C10 delegates the field list to no longer agree. |
| C11 | conforms | Facets may add classes, payload forms, and stronger delivery evidence and may not reinterpret session/interaction identities, authority decisions, the four terminal provenance forms, or effect uncertainty; retry is a new interaction identity with optional attributable causation and never replay; the intra-interaction ordering fact is named as the one ordering fact core owns, which a facet may strengthen and may not weaken. A falsification attempt (probe **P6**) failed: no facet route reaches a core identity, authority, terminal-provenance, or uncertainty result, and cross-capability invariant 7 and the matrix's `Extension hooks` list agree with C11 rather than restating it loosely. |
| C12 | does-not-conform | C12's own rule is what **AJ1** violates: "Every property must be able to fail against a named incorrect implementation." At this pin `C4-P2` can be evaluated green on `C4-outcome-precedes-ack` from the field list two of the five publishing artifacts carry, so the rule is unsatisfied for the one property this programme has spent ten cycles on. AE3's converse rule is stated in C12 in the terms that make it a rule, the brief's format carries the required-green set as a normative field, and AF7's audit extension holds — I enumerated it independently at 12 capability rows + 13 state-machine rows = 25 audited against 13 C-properties + `S1`-`S6` + `I1`-`I7`, with the `C4` row registering both C4 properties (probe **P4**). The audit's honesty about `I1`-`I7` satisfying neither half is the right disposition and is disclosed residual work; **AI3 is closed** — the `I5` cell now carries the AE3 connection and the `C4` cell's pointer names its direction correctly. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Ten legal transition rows, a refused/illegal table, a totality rule that explicitly does not override named nonfatal rows, and `S1`-`S6` carried in the property audit under AF7. No external phase appears as a session state and each is listed as explicitly not one. Reconnect creates a new session identity and begins at `unestablished`, inheriting no replay or in-flight state — the artifact that actually carries the fact AI5 found misattributed to C2, and AI5 is correctly closed by withdrawing the citation. Drain is symmetric and occurs exactly once per endpoint history. Status block reaches `AI9` and is accurate: this artifact genuinely is unchanged by the AI family. |
| Interaction state | conforms | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; `I1`-`I7` hold as statements and are audited. The `unseen` row is a detailed row (X3) routing to `unseen` rather than to the terminal state (Y3), carrying `known-none` (AE2), the refused frame's kind and detailed reason (AC2), and no history, latch, or reservation (W4). The latch section's settling-frame list is one of the three correctly carrying the session at this pin; **AJ6** is against the justification sentence beneath it, not against the list. |
| State/event totality | does-not-conform | The totality property itself is sound and I verified it independently (probe **P2**): 6×6 session, 6×6 initiator, 6×6 recipient published rows = **108 published-row cells**, zero empty; expanding the published groups against the machine's own state tables (12 initiator states of which 6 terminal, 12 recipient states of which 7 terminal) gives **180 underlying state/event pairs**. Agrees with reviews 7-13. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event so rule 2 cannot produce the terminal `peer-fault` W4 refuses, and the `not-applicable` latch is asserted as a value rather than an absent field. The verdict turns on **AJ1**: this artifact's Late-traffic latch section publishes the settling-frame reference without the session, its Evidence-required section makes that reference part of what every generated cell asserts, and its status block declares the artifact unchanged by the family that changed the reference. |
| Responsibility | does-not-conform | Enumerated mechanically (probe **P4**): **39 ownership rows, 22 distinct owner identifiers, every row carrying exactly one backticked owner, zero rows with two owners or none, and no `channel-core` in any owner cell** — it survives only in the prose recording that U2 abolished it. The `Intra-interaction frame order` row is owned by `channel` with the realization profile's declaration as its crossing artifact. The verdict turns on **AJ1**: the `Local observation content and provenance` row is the declared owner of the fact and its crossing artifact does not carry the field the parity profile now compares, which the artifact's own status block asserts can never be the case. |
| Completeness | conforms-with-nonblocking-findings | AG1 remains closed in the silence-probe row, which names the complete record set both endpoints produce. **AH2 and AI3 are both closed**: the property audit's `C4` required-green cell names all seven members, the `I5` cell carries the AE3 exposure, and both pointers name their direction correctly. The disposition history is accurate and runs to the thirteenth independent review; the residual risks are stated as challenges rather than resolutions; the AF7 audit extension is complete over all 25 rows and its `owed` cells are honest. Status block reaches `AI3` and is stale about its own history length (**AJ4**). |
| Migration coverage | conforms-with-nonblocking-findings | All 24 predecessor vectors dispositioned CH-01 through CH-24 in order, verified against `conformance/channel-0.1-vectors.json`, which holds exactly 24 (`CH-01-CORRELATION-ECHO` … `CH-24-FAILURE-DOMAIN-RELATIVITY`). Twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. AE5's retained requirements register is in the sources inventory; `CH-R10` is dispositioned **replaced** with `CH-K5` **retained**; AF4's admission is in the new-evidence inventory; Z4's intra-interaction frame order and both mutations are listed. The new-evidence inventory's settling-frame entry is the third surface of **AJ1** and is why this is not a plain `conforms`: it is the list Batch 2 builds vector groups from, and it names the reference without the session. |
| Neutral brief | conforms-with-nonblocking-findings | The two field lists this document publishes both carry the session and are correct; **AH1's and AI1's halves in this artifact are closed**, and probe **P3** confirms both. Everything else holds: artifact boundaries, identity spaces, the three-version rule, the closed operator set with W1's precedence relation, AG2's session qualifier, and Z1's identification-only restriction on the arrival ordinal, the required-green set as a normative format field, the golden policy, the reordering-injection provider boundary with W2's present-not-true point, and the Batch 2 entry gate. **AJ3** is against the vector-format bullet and **AJ4** against the status block; the subordination sentence at lines 31-34 is what makes **AJ1** blocking rather than a wording difference, and it is correct as written. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; the grid's cancellation columns; the matrix's `Concurrency and cancellation` boundary ruling. The completeness review's direction-scope row records the session-wide-versus-per-direction disagreement and its AE3 relation, and both audit cells now point at it (AI3 closed). |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan's ruling and the matrix's boundary ruling; ledger `ready` → **moved** as state, message kind, and feature. No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B including its explicit rejection of C and D, its composition-root standing-in, and its refusal to introduce a Component-to-Component binding kind, with the envelope-kind departure disclosed and reasoned. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's sentence that a facet may strengthen the intra-interaction ordering fact but not weaken it is the one place the S1 ruling touches this ruling, and the two are consistent. |

The plan's `## Open questions (owners needed)` section correctly reports no unresolved owner decision.
The R1 (2026-08-13), S1 (2026-08-13), and AE1 (2026-08-14) correction rulings are each recorded as
correction rulings that do **not** join the fixed set of four. AG3 remains closed: the AE1 ruling states
the membership operand "within one session" and carries the "Issued with a vector-scoped operand,
narrowed to the session under AF8 on 2026-08-15" note, retaining the original wording — the same
treatment the S1 ruling gives `channel-core`.

**The 2026-08-15 closure-standard ruling.** I read it in the plan (lines 629-651) rather than taking the
dispatching brief's summary of it. It is recorded as a first-batch ruling on the closure standard that
does not join the four design rulings; it selects "only `conforms` closes"; it records the rejected
alternative with reasons; and it states plainly that it was made after a verdict it excludes and why
that timing is disclosed rather than left unremarked. It is represented consistently in the plan, the
review policy's status paragraph and step 3n, the completeness review's disposition history, and the
Channel index's plan row. It governs the consequence of my verdict and did not affect which verdict I
reached: **AJ1** is blocking on the programme's own applied standard, not on the new one, and my verdict
would be `does-not-conform` under either.

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
  rows. **S3 is now closed on all four surfaces its evidence quotes**, including the plan's §7.8, which
  AI9 reopened and this commit corrected; the Channel index's 108-cell count is present and I confirmed
  it by enumeration. Its class recurs on the narrative surfaces as **AJ2**.
- **U1-U8** — closed. U1 at the property, at C4's vector passage, and at the completeness review's
  account of the same vector. U2-U8 closed. **U4 is closed in the disposition history and its own status
  block still claims the eighth cycle** (**AJ4**). **U6's pin clause is now true as to subject and as to
  date**, the date corrected by `129122c`.
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared and bounded to mutation vectors; the precedence relation restricted to one endpoint's own
  declared steps within one session; the reordering provider's declaration with the present-not-true
  point; second mutation added and placed in a required group; retention rule in C4, the machine, and
  the grid; committing-endpoint operand supplied; latch compared; settling frame recorded and compared;
  `not-applicable` owned; `unseen` transition row present; recording-versus-retaining distinction;
  iteration reviews retained; C10 and the schema carrying the latch and settling frame; the refusal
  leaving state at `unseen`; the arrival ordinal restricted to identification; the grid naming a
  provenance as a provenance; the ordering requirement in the new-evidence inventory. **Y4's ordinal
  argument is damaged in the machine by the AI1 insertion** (**AJ6**).
- **AA1-AA3, AB1-AB2** — closed as framed. Both indexes carry every disposition family somewhere, both
  computed counts read 13, `channel-core` appears in no status entry point, and the matrix owns local
  observation content. **AA2's narrative half is AJ2**, for the third cycle.
- **AC1-AC4** — **AC1 is the one to read next to AJ1.** Its correction added the arrival ordinal to the
  brief, the interaction machine, the grid, and the matrix, and the verifier carries a check for each of
  those four. The AI1 correction added the session to two of the same four. AC2, AC3, and AC4 are
  closed; **AC3's pronoun class recurs as AJ6's cause**, one paragraph from where AI6 corrected the
  previous instance.
- **AD1-AD3** — closed, AD2 by the ruled correction which AF6 replaced with the declared provenance
  table. The table classifies every family the policy bolds, including `AI`, and every `iteration`
  family has its retained record.
- **AE1-AE5** — closed. AE1 at the property, the parity profile, the contract's vector passage, and the
  completeness review's silence-probe row; AE2 in both artifacts and both grid cells; AE3 as a rule in
  C12 with the format field and the audit column; AE4 and AE5 on both surfaces each.
- **AF1-AF8** — closed. AF1 on both surfaces, verified by evaluator (**P3**, rows M1 and M1b). AF3
  verified against the register's own highest identifiers (`CH-R` 11, `CH-K` 7). AF5 closed on all three
  surfaces. AF8 closed at the membership operand in both normative artifacts and in the ruling of
  record. **AF2's class on the artifacts' own status blocks is AJ4.**
- **AG1-AG5** — closed. AG1 verified by evaluator: a vector authored from the silence-probe row as it
  now reads takes `C4-P2` red on its own named mutation. AG2's qualifier present in the operator with
  the claim pinned. AG3's ruling note present in the S1 ruling's form. AG4's nine artifact rows each
  making a claim about `AI` — **but four of them make it about `AI9` rather than the family, which is
  AJ5, and two of those four are wrong**. AG5's `| Channel |` row naming AI1-AI9.
- **AH1-AH6** — closed. AH1's session on the declared stimulus step and the multi-session declaration
  are both present and probe **P3** confirms the two-session conforming vector green. AH2's
  required-green cell names all seven. AH3's three narrative surfaces — **one corrected, two still
  open, which is AJ2**. AH4's escape clause bound, **and bypassable as AJ5 records**. AH5 closed in both
  audit rows. AH6's coverage limit stated in both artifacts and confirmed by probe **P3** row `R`.
- **AI1** — **closed in three of five publishing artifacts and open in two** (**AJ1**).
- **AI2** — **open in both artifacts its evidence names** (**AJ2**).
- **AI3** — **closed.** The `I5` required-green cell carries the AE3 exposure in the same terms as the
  `C4` cell, and both pointers now say "in the silence-probe table above", which is the correct
  direction.
- **AI4** — **closed as to family currency across all eleven status blocks, open as to both quoted
  sentences** (**AJ4**).
- **AI5** — **closed.** The citation of "C2's reconnect and new-session cases" is withdrawn rather than
  repaired, and the identity argument that remains stands on its own. I checked C2's Silence and the
  session machine independently and agree the ruling is correct on the grounds it now gives. No other
  artifact carries the withdrawn citation.
- **AI6** — **closed as to substance.** The contract now names the recipient's subsequent admission as
  the conjunct's subject instead of relying on the nearest antecedent. **AI6's own class recurs as
  AJ6.**
- **AI7** — **closed in the parity profile, open in the vector format** (**AJ3**).
- **AI8** — **closed**, by `129122c`, one commit after the check written for it fired on its own first
  run. This is the cleanest correction in the commit and worth saying so.
- **AI9** — **closed on both surfaces.** The plan's §7.8 now reports thirteen retained attestations,
  twelve `does-not-conform` and one `conforms-with-nonblocking-findings`, names the ruling that kept the
  latter from closing the batch, and records its own six-cycle staleness. The verifier's
  `all twelve negative attestations` message is now `all thirteen retained attestations`, and I saw the
  corrected message myself when the gate rejected this attestation's filename.

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 865 local links across 306 documents |
| `build/verify-text.ps1` | pass — 885 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised for
this review.

**Green gates are not evidence of conformance**, and this review found that again: all seven findings
sit behind a fully green design gate, including the blocking one. **AJ1** sits behind the AI1 check,
which iterates two artifacts and asserts its own completeness with `Count -lt 3`, and behind four AC1
checks that already enumerate the correct four artifacts for the same reference. **AJ2** sits behind the
AA2 family check, which asks only that a family appear somewhere in `docs/future/README.md`, and behind
the AH3 check, which reads one sentence of that file and the plan's status block. **AJ3** sits behind
the AI7 check, which reads the corrected half of its own finding. **AJ4** sits behind the new AI4 check,
which requires only that the block name the newest family. **AJ5** is a check passing a row it was
written to fail. **AJ6** and **AJ7** have no check.

### P2 — independent enumeration of the state/event grid

Parsed mechanically from the grid's three tables and cross-checked against the interaction machine's own
state tables rather than against the grid's prose counts.

- Session: 6 states × 6 event columns = **36** cells, 0 empty.
- Initiator: 6 published state groups × 6 columns = **36** published cells, 0 empty; the machine states
  12 initiator states (`candidate`, `admitting`, `refused-local`, `dispatched`, `cancel-pending`,
  `cancel-accepted`, `cancel-refused`, three Outcome terminals, `peer-fault`, `lost`; 6 terminal),
  giving 12 × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells, 0 empty; the machine states 12
  recipient states (7 terminal), giving 12 × 6 = **72** underlying pairs.

**108 published-row cells, 0 empty, 180 underlying state/event pairs** — agreeing with the seventh
through thirteenth reviews. No cell offers a choice between two routes, and the closed-world rule
ordering is well-founded.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`), and the positive result AJ1

The policy requires at least one genuine attempt to falsify a capability-wide property. An evaluator was
written from the published prose — `C4-P2`'s two conjuncts with AC3's committing-endpoint subject, the
AE1 admission clause and AF8's session scope, the brief's closed operator set including AG2's session
qualifier on precedence, the brief's vector format as AH1 rewrote it, and the settling-frame reference
**as each of the five artifacts that publish it states it**. It imports no repository code. Precedence
is implemented exactly as the brief declares it and returns *no verdict* outside its scope rather than a
default; the arrival ordinal is used for equality only and never as an ordering operand.

The design's own claim about its operand was made the variable rather than assumed: the same vectors
were run five times, once per published field list, with nothing else changed.

| Vector | Design expects | brief §local obs | brief §parity | machine §latch | **grid §latch** | **matrix row** |
| --- | --- | --- | --- | --- | --- | --- |
| 1-2. conforming commit-order delivery, both directions | green | green | green | green | green | green |
| 3. request lost, control delivered (AE1's member) | green | green | green | green | green | green |
| 4. acknowledgement lost | green | green | green | green | green | green |
| 5. control for an identity the peer never opened | green | green | green | green | green | green |
| 6. legal late control after a peer's terminal | green | green | green | green | green | green |
| 7. duplicate terminal from a nonconformant peer | green | green | green | green | green | green |
| M1. `C4-control-precedes-request`, obs per C4's passage | red | red | red | red | red | red |
| M1b. same vector, obs per the completeness silence-probe row | red | red | red | red | red | red |
| M2. `C4-outcome-precedes-ack`, single session | red | red | red | red | red | red |
| P. wholly conforming two-session identity reuse | green | green | green | green | green | green |
| R. reordering whose displaced request is refused on its merits | green as stated | green | green | green | green | green |
| **M2-two-session. `C4-outcome-precedes-ack` + conforming reuse** | **red** | **red** | **red** | **red** | ***green*** | ***green*** |

Five results matter.

1. **`C4-P2` is sound in both directions under all three corrected field lists.** Green on all seven
   legal members of its required vector group, red on both named mutations, green on the two-session
   conforming vector. AE1, AF1, AF5, AG1, AH1, AH2, AH6, and AI1's analysis all hold.
2. **`M2-two-session` is green under the grid's list and the matrix row's**, which is AJ1 and is the
   same false green review 13 raised, at the same vector, produced by the same mechanism, from two
   artifacts the correction did not reach.
3. **Row R is AH6 working**, and row M1b confirms AG1 stays closed.
4. **Restricting both conjuncts to one endpoint's own frames remains load-bearing**, confirmed rather
   than assumed: member 6 is green only because the settling frame and the terminal frame have different
   committing endpoints, and member 7 only because the ordinal binds the settling frame to the *later*
   of the two matching steps.
5. **The single-session mutation is red under every list**, which is why no gate and no narrower probe
   catches AJ1: the defect is invisible except in a vector class the required groups do not yet contain
   and the design newly permits.

### P4 — mechanical enumeration of ownership, the property audit, the registry pins, and the corpus

- **Responsibility matrix, enumerated from the source:** 39 ownership rows, 22 distinct owner
  identifiers, every row carrying exactly one backticked owner, zero rows with two owners or none, and
  `channel-core` in no owner cell.
- **Property audit, enumerated:** 12 capability rows + 13 state-machine rows = 25 rows, against 13
  C-properties + 6 `S` + 7 `I` = 26 properties, the `C4` row registering both `C4-P1` and `C4-P2`.
  Complete.
- **Registry pins, all twelve path/hash pairs in `Brontide-Architecture-Status.json` recomputed** — the
  Architecture 0.8 document, the 0.5 implementation baseline requirements, the 0.8 requirements, both
  stacks' 0.5 matrices, both stacks' 0.8 matrices, both stack READMEs, and both stack milestone-evidence
  ledgers — **all twelve match**.
- **Register ranges computed from the register itself:** `CH-R` highest = 11, `CH-K` highest = 7,
  matching the ledger's claimed range.
- **`conformance/channel-0.1-vectors.json` holds exactly 24 vectors**, `CH-01-CORRELATION-ECHO` through
  `CH-24-FAILURE-DOMAIN-RELATIVITY`, and the ledger dispositions CH-01 through CH-24 with no gaps.
- **Every design artifact's status block extracted and checked for family currency**: all eleven reach
  `AI`. This is the AI4 check working; **AJ4** is what the same enumeration finds when the blocks are
  read rather than pattern-matched.

### P5 — running the AI1 check's own regex over the artifacts it does not read (positive result)

The AI1 check's pattern, `(?:its|frame's) kind, its(.{0,95})`, was applied to all four artifacts that
publish the settling-frame reference:

| Artifact | field lists matched | session present |
| --- | --- | --- |
| neutral brief | 2 | both yes |
| interaction state machine | 1 | yes |
| **state/event coverage grid** | **0** | — (the list exists; the pattern does not reach it) |
| **responsibility matrix** | **0** | — (the list exists; the pattern does not reach it) |

Then the mutation the check was written for: removing `its **session**,` from the brief makes both of
its lists fire. **The check is sound over the two artifacts it reads and blind to the other two, in both
directions** — it would not fail if their lists were deleted entirely. Its `Count -lt 3` guard is set to
exactly the number of lists in its own scope, so it certifies completeness against itself.

### P6 — attempts to falsify `C2-P1`, `C8-P1`, and `C11-P1` (negative results)

`C2-P1` asserts that every accepted session transition belongs to the published table, every other input
leaves the prior state unchanged or enters `faulted`, and no terminal session returns to a nonterminal
state. The sharpest available case is the closed-world rule ordering producing a route out of `closed`
or `faulted`, since rule 2 makes a recognized peer event in a *nonterminal* state a `state-violation`
and rule 5 governs terminal input. **It does not fail**: all twelve cells of the `closed` and `faulted`
rows read "terminal late input" or "remains `closed`/`faulted`; local observation only", rule 5 states
terminal input "never reopens a terminal history", and rule 1 gives the detailed rows precedence so no
cell offers a choice.

`C8-P1` asserts at most one accepted terminal history per interaction and that no cancellation control,
drain, timeout, or protocol rejection is recorded as semantic success. The sharpest case is T3's
`cancelled` Outcome with no cancellation request in force, which is a semantic Outcome form arriving on
a history that cannot accept it. **It does not fail**: the recipient commits `internal-channel-failure`
instead of the Outcome and the initiator records a peer fault, so neither endpoint records it as
success, and every duplicate-terminal route in both grids preserves the first accepted history under the
late-traffic latch.

`C11-P1` asserts every established profile has all required facets supported exactly and no facet
changes a core identity, authority, terminal-provenance, or uncertainty result. The sharpest case is a
Distributed facet declaring delivery ordering, since C11 explicitly lets a facet add ordering guarantees
and C4 owns intra-interaction frame order. **It does not fail**: C11 names the one ordering fact core
owns and says a facet "may add delivery and ordering guarantees beyond it but may not weaken it", and
retry is bound to a new interaction identity so a facet cannot reach interaction identity through the
retry route.

Recorded because a failed falsification attempt is evidence and an unrecorded one is not.

### P7 — upstream consistency and clone completeness

- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with `latestRatifiedArchitecture` null and the
  rationale "No Brontide architecture document currently has Ratified status." The document's own header
  carries the same Complete Draft status and states that neither implementation evidence nor an
  experiment changes its ratification status.
- Both stacks state `**Designed for:** Brontide Architecture 0.8, Complete Draft, not ratified` and
  `**Status:** Partial implementation with explicitly labelled experiments`. The Channel 0.2 contract
  states `Designed for: Brontide Architecture 0.8, Complete Draft` and the plan `Designed against:
  Brontide Architecture 0.8, Complete Draft`. No artifact treats 0.8 as ratified or claims Channel 0.2
  implementation conformance, and every first-batch status block carries T4's stable phrase with no
  superseded cycle name.
- Decision 13's recorded ruling (Option A retained for 0.1, Option B selected for 0.2, C and D rejected,
  recorded 2026-08-11 by user:JakHoh) matches C3, C7, and the plan's relational-initialization ruling,
  including the composition-root standing-in and the refusal to introduce a Component-to-Component
  binding kind.
- PB8's blocking finding in both stacks — process loss fabricating a known zero effect count — is
  answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect, and the
  ledger moves `providerEffectCount` to the Portable Binding/domain owner. PB8's attestations and their
  reviewed commits are present and untouched.
- The retained Channel 0.1 design note, draft contract, and requirements/risk ledger are present,
  inventoried in the migration ledger's sources list, and unmodified by this correction sequence.
- `channel/0.2` does not exist — indeed no `channel/` directory exists: no neutral schema, vector,
  property, or golden has been authored, and the Batch 2 entry gate in the brief is unchanged.
- 891 tracked paths, empty `git diff HEAD`, clean status, HEAD at
  `6cddb990f1f8aada3018a19c63b43116b83f05e6`. No design artifact was read from outside the clone.

## What this verdict means

**Seven of the nine AI corrections land, and two of them land cleanly in a way worth naming.** AI8 was
corrected by its own check firing one commit later, which is the first time in this programme a
staleness class has been caught by machinery rather than by a reviewer two cycles on. AI9 closed a
retained finding that had been open for six cycles, on both of its surfaces, and the plan's §7.8 now
records its own staleness rather than merely being corrected. AI3, AI5, and AI6 are each right, and
AI5's disposition — withdrawing a citation rather than repairing it, because the argument stands without
it — is the correct call and not the easy one. The AI1 analysis is right in every respect except its
count of where the fact is published.

**The verdict turns on one escalation I did make and several I did not.**

- **AJ1 is the escalation.** It is a demonstrated false green on a named mutation, reproduced from two
  artifacts' own field lists, in the commit whose stated method is "when a correction changes a fact,
  the impact set is every artifact asserting something about that fact". Applying that method to this
  fact gives five artifacts; the commit applied it to three and the check to two. Every previous false
  green in this programme was blocking. The counter-argument — that the grid's latch paragraph and the
  matrix's crossing-artifact column are derivative of the machine — is stated in full under AJ1 so an
  owner who accepts it can see exactly what they are accepting. **If it is accepted, my verdict should
  be read as `conforms-with-nonblocking-findings`**, which under the 2026-08-15 ruling still does not
  close the batch. I do not accept it, because AC1 was raised, ruled, and checked on the opposite
  reading of the same five artifacts about the same reference.
- **AJ2, AJ3, and AJ4 are the same shape at three scales** — a finding closed in the surface a check can
  reach and left open in the surface its evidence quoted — and all three are rated nonblocking on the
  unbroken precedent for entry-point and record staleness. **AJ2 is the closest of the three**, because
  it is not only staleness: the plan asserts that AI2 is corrected there, and AI2's two artifacts are
  unchanged. An owner who holds that a false closure claim about another artifact is AG2's class and
  therefore blocking would escalate it; the programme has rated AG2 itself nonblocking, so I have not.
- **AJ5, AJ6, and AJ7** are a check bypassed by a wording it did not anticipate, an argument broken by
  where a word was inserted, and a list out of order. None changes a property verdict.

**What I would tell the next pass to read first is the artifact set, not the fix.** Six findings across
three cycles — AF8, AG2, AH1, AI1, AI7, AJ1 — are one question asked six times: which of `C4-P2`'s
operands need a session, and does *every* artifact that publishes each of them give it one? The
correction pass has now answered "which operands" correctly three times running and "every artifact"
incorrectly three times running. The list for the settling-frame reference is fixed and short and is
already enumerated in the verifier: the brief's local-observation schema, the brief's parity profile, the
interaction machine's latch section, the grid's Late-traffic latch section, and the matrix's
`Local observation content and provenance` crossing artifact — the four the AC1 checks at lines 540-563
already name, plus the brief's schema. The migration ledger's new-evidence inventory is a sixth surface
that states the same reference for Batch 2's benefit. A check that iterates those and asserts an exact
count is the durable form; a check that iterates two and asserts `Count -lt 3` is a check scoped to the
artifacts that were already corrected, which is what let AF1 survive its own correction twice.

**The second thing worth carrying forward is that the sweep axis changed and the sweep did not.** The
commit message's diagnosis is right and is the best statement of the class this programme has produced.
What it did not change is who computes the impact set: the same pass that decides a fact has changed
also decides which artifacts assert it, from memory of the artifacts it has been editing. AI1's own
evidence section listed the two artifacts it had read; the sweep reproduced that list and called it the
concept. The mechanical version of the concept sweep is a search — for this fact, `grep -n "settl"`
across `docs/future/channel/` returns all six surfaces in one screen, and I found AJ1 by running it
before reading anything.

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

The gate results in **P1** are from before this attestation existed. I re-ran it afterwards and it fails
with exactly the three failures the ninth through thirteenth reviews recorded, and nothing else:

```text
FAIL: The Channel 0.2 design foundation must retain exactly the review README, all thirteen retained
      attestations, and all four correction iteration reviews before the next closure review.
FAIL: The Channel index's Design reviews row does not say '14 retained attestations' …
FAIL: The future-work index does not say '14 retained independent reviews' …
```

That is the verifier working as designed — and the first message is where I confirmed AI9's second
surface closed, since it reads "thirteen **retained**" rather than the "twelve **negative**" AI9 raised.
Because this verdict does not conform, the correction pass does **not** additionally have to change what
"independent review still pending" or the `awaits a fresh independent closure re-review` phrases assert.
`git status --porcelain` after writing this file lists exactly one untracked path — this attestation —
and nothing modified.

Four notes for that pass.

First, **AJ1**'s fix belongs in the grid's Late-traffic latch section and the matrix's crossing-artifact
column, and its check must iterate the *reference*, not an artifact list written from a finding's
evidence. The AC1 checks at lines 540-563 already enumerate the correct four artifacts for this exact
reference; the durable form is one loop over that set asserting both the ordinal and the session, with
an exact-count guard rather than a lower bound. A guard set to the number of lists in its own scope
cannot report that its scope is wrong.

Second, **AJ2** will survive a token substitution for the eighth time unless the two narratives are
rewritten rather than the counts. The specific sentences are `docs/future/README.md` lines 62-68 (which
must introduce the eleventh, twelfth, and thirteenth reviews before referring to their families) and
`docs/future/channel/README.md` lines 13-17 and 43 (which must reach past AC1's description). The plan's
status-block claim to have corrected AI2 should be withdrawn or moved to the artifacts AI2 names.

Third, **AJ4** and **AJ5** are both checks that pass on a string rather than on the claim. The AI4 check
should additionally reject a status block whose own self-description names a cycle count lower than the
disposition history's length, and the AG4/AH4 row test should require the family without a following
digit — `unchanged by[^|]*\bAI\b(?![0-9])` on both clauses — so a row cannot discharge a family
obligation by naming one finding of it.

Fourth, and this is the one that should change how the next pass works rather than what it asserts: the
sweep should be executed as a search over the repository for the changed fact's own vocabulary, with the
result recorded, before any artifact is edited. Every one of the last three cycles' blocking findings —
AG1, AI1, AJ1 — was one `grep` away from the pass that missed it.
