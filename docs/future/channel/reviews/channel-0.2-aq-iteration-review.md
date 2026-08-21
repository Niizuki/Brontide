# Channel 0.2 fifth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-fifth-pass-2026-08-21-138af11`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), W3 (the status blocks and the
Channel index rows), and the guard corpus AO3 retained — at `138af11`,
`Merge pull request #135`; raised and dispositioned the AQ1-AQ5 findings this document records

Date: 2026-08-21

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the **fifth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it either.** It raised five findings against eight probes, and the
count has stopped falling — three, six, three, two, five.

## Method

**It began by running the corpus, as the AP review asks of it.** One command, 53 of 53 green. That is
the second cycle in which AO3 has paid, and this pass adds nothing to the argument the AP review
already made for it.

The brief it inherited was one sentence: **every check written before W2 and W3 owes the question *is
its key still load-bearing?*, and only three of those keys have been tested.** AP probed three
phrase-keyed blocks by hand and read the rest. Reading is what four passes have already done to these
files, so this pass did not read them again. It built an instrument instead.

**The instrument is a coverage trace.** A guard whose key has expired does not announce itself, but it
has one property that is mechanically visible: **its body never runs.** Each gate was executed under
`Set-PSDebug -Trace 1`, the executed line numbers were collected, and every statement in the script's
syntax tree whose line never appeared was reported — minus the `$failures.Add` sites, which a green
gate is supposed not to reach. What is left is the set of checks that are present in the file and
absent from the run.

It reported 42 such statements in the design verifier, 48 in the properties gate and 24 in the facts
gate. Most are what they should be: array-literal continuation lines, the `-Apply` rewrite path of the
facts gate, `catch` blocks, the `else` arm of a hold state that is not current, and the red branches
of a property that is green. **Five were checks that no longer run at all**, and all five are AP1's
class: a key that was correct when it was written and stopped being correct when the work moved.

The trace does not find everything — a check whose body runs while a *negative* assertion inside it
under-reaches is invisible to it, and **AQ5** is that case, reached from the two large proximity
windows the trace pointed at rather than from the trace itself. Both directions are recorded below.

## Findings

### AQ1 — the narrative freshness check has measured nothing since 2026-08-20 — corrected

**This is the most serious finding this verification work has produced, and it is measured by what it
guards rather than by what it is.** The AJ2 check requires each of the three entry-point narratives —
the Channel index, the future-work index's Priority 1 passage, and the disposition index's section for
the redesign plan — to name every closure-review finding family and to introduce each numbered review
*by its ordinal*. It exists because those narratives went stale for eight consecutive cycles, every
correction was a token substitution, and AJ2 was the finding that no check read a narrative at all.

It reads the finding-family provenance table with

```
'(?m)^\| ([A-Z]{1,2}) \| closure-review \|([^|]*)\|'
```

and looks for `closure review <n>` in the second captured cell. On 2026-08-20 the owner ruling that
made each family declare what it was raised **against** inserted `Raised against` as the table's third
column. Every row has since yielded ` design ` where the record name used to be, no row has matched
`closure review <n>`, every one has taken the `continue`, and **the entire check — three narratives ×
every closure-review family, both the family assertion and the ordinal assertion — has been an empty
loop.**

It went dark in `6a6c76d`. Three iteration passes have run since, and none of them noticed.

Probed at the parent commit: the ordinals for reviews 13, 14 and 15 removed from all three narratives,
every family token left in place — the exact defect AJ2 names, *"a narrative that jumps from the tenth
review to a family raised by the thirteenth"* — and **both gates green.** The retained probe is
`AQ1-a`, which is the single-file, single-ordinal form of it.

**Corrected by taking the key off the table's shape.** The numbered attestations the reviews directory
*holds* are the set the narratives owe; they exist on disk independently of any prose, and a family is
now looked up for each of them. A row that names no review, a column inserted, a header renamed or a
cell reworded now produces a failure naming the review it could not attribute, instead of silently
shortening the loop. That is the AP1 correction pattern — an absent claim made loud rather than
silencing — applied to the check that most needed it.

### AQ2 — the settling-frame publication count moved out of the swept file set under W3 — corrected

AK4's count claim is checked against the fence registry rather than read: the design verifier counts
the artifacts that publish a frame reference and compares that against any sentence saying *"N other
artifacts that publish"*. It sweeps the nine design artifacts and the review policy.

**W3 moved the claim out of that set.** It lived in the migration ledger's status block; W3 moved every
status block's correction history verbatim into the disposition index, which is a retained review
record and not a design artifact. The sentence went with it and has been unread since.

Asking the AN pass's question — *where else is this stated* — then found the count a **second** time in
the same file, in the index's row for the ledger, worded **"the four other publishing artifacts"**.
That wording matches no phrase the sweep keys on, so it would have escaped even in the right file.

Both are correct today. Both were unguarded: `AQ2-a` and `AQ2-b` set each to `nineteen` and the gates
stay green. **Corrected** by sweeping the disposition index alongside the design artifacts and by
keying to the claim — all three wordings the count is stated in — rather than to the one sentence that
first carried it.

### AQ3 — the disposition-history length claim moved with the history it is about — corrected

AJ4 is the completeness review's status block saying its disposition history ran to the eighth cycle
when it ran to the thirteenth. The check reads the nine artifacts' status blocks for *"runs to the Nth
cycle"* and compares the ordinal against the number of retained review cycles.

**W3 emptied those blocks.** No status block has carried the phrase since, the check has read nothing,
and the claim is live in the record the history moved to. `AQ3-a` sets it to `eighth` with the gates
green.

**Corrected** by reading the moved status text in the disposition index as well as the blocks. The
boundary is what makes this checkable and is worth stating: a `Status:` paragraph is the document
speaking about itself in the present, which is the whole of what AJ4 is about — *"a status block that
understates its own document"* — while the disposition paragraphs beneath it are history, and history
recites old counts deliberately. The index says in one breath that a block once said the history ran to
the eighth cycle, and in the next that it runs to the sixteenth. Sweeping the file whole reads the
recital as a claim; sweeping the `Status:` paragraphs reads the claim and leaves the recital alone,
without asking the check to tell a past tense from a present one.

**This pass wrote a probe against that recital and the probe was wrong.** It mutated a frozen statement
of what the AL correction did into a false one, which no freshness guard owes an answer about. It was
deleted rather than kept, under the corpus's own authority note: where a probe and an artifact disagree
about what the design says, the artifact is right and the probe is the defect.

### AQ4 — the guard against an affirmative false claim about review coverage never matched — corrected

AH3 requires that the future-work index's sentence *"No independent review has yet seen the X
corrections"* name the newest family. Its own failure message states why it is worth having: an
affirmative false claim about review coverage is worse than a stale one, because it tells a reader the
newest work is unreviewed when it has been reviewed and corrected.

The pattern is a prose sentence and was matched against the **raw** file. The sentence now wraps
between `No independent` and `review has yet seen`, so the match stopped happening and the check went
silent. `AQ4-a` names the wrong family with the gates green.

This file has carried `Get-FlowedText` since its first prose assertion, under a comment saying that a
normative sentence which reflows across a wrap is still the same sentence. The check that most needed
it is the one written without it. **Corrected** by flowing the text.

### AQ5 — three keys are a character count, and the artifact outgrew all three — corrected

A proximity window is a key like any other, and it expires the same way — silently, as the passage it
spans grows.

- **The properties gate's member-count claim.** It reads *"required vector group has N legal members"*
  within 4,000 characters of the property's own marker. C4's passage has grown: the marker and the
  count now sit **5,246** characters apart, so the check has not run. `AQ5-a` contradicts the declared
  seven-member set with the gate green. Corrected by bounding the region with the contract's own
  structure — the next property marker or the next capability heading — so it grows with the passage
  it is about.
- **AF1's expected-observation passage**, captured at 900 characters. The passage is longer. The
  *positive* assertion still passes, because its subject is early; the *negative* one — that C4 must no
  longer say the expected observations are "exactly what the receiving endpoint records" — policed only
  the first 900 characters. `AQ5-b` restores AF1's own superseded wording at the far end of AF1's own
  passage, and the gate is green. That is AF1 reproduced verbatim by a guard written to close it.
- **AG3's dated AE1 ruling**, captured at 2,600 characters. The ruling runs to **6,923**, so the
  assertion covered 38% of the passage it names. `AQ5-c` restores the pre-AF8 operand scope among the
  rejected options, inside the ruling, past the boundary, with the gates green.

**The general shape is worth stating, because it is what the next pass should hunt.** A
character-bounded window fails *safely* for an assertion that something must be **present**: truncation
makes the check fail loudly. It fails *silently* for an assertion that something must be **absent**,
because the forbidden text simply sits past the boundary. **A negative assertion must never be
evaluated over a window a character count bounds.** Both of the ones in the design verifier were.

Both are now bounded by the end of their own subject — the sentence naming the witnesses `C4-P2` fails
on, and the next dated ruling — and each boundary is *asserted* rather than assumed, so a region with
no declared end fails instead of quietly running to the end of the file.

## What this pass verified rather than believed

- **The retained corpus runs clean**: 53 of 53 before this pass's work, in one command.
- **Each finding was pinned before it was fixed.** All eight probes were written into the corpus first
  and run against the unmodified guards, and each returned `pass` where its guard owed `fail` — the
  gate green on the guard's own subject. Only then was any guard touched.
- **The trace instrument was checked against a known answer.** It reports the `-Apply` rewrite path of
  the facts gate and the red branches of a green property as unexecuted, which is correct and is the
  calibration: it finds unexecuted code, and telling an expired key from a dormant-by-correctness one
  is the reading this pass did on each of the 114 statements it reported.
- **Two of the reported checks are dormant rather than expired**, and are left alone. The AK3
  "N capabilities owe" claim has no live surface because the residual work it counts is finished, and
  the `elseif ($dispatchMarkerPresent)` arm is the branch for a hold that is not current. A guard with
  nothing to say is not a guard that has stopped working.
- Eight probes were added to the corpus, one per finding surface, which is the practice AO3's open
  question asks about — a pass that touches a guard adds its probe.

## What this pass did not do

It did not re-read the design; condition 4 scopes it to the verification work.

It did not extend the trace instrument to the branch level. It finds a check whose body never runs; it
does not find a check whose body runs while an assertion inside it under-reaches, which is AQ5's shape
and was found by reading the windows the trace pointed at. **A branch-level or mutation-level coverage
measure over the gates is the obvious next instrument**, and it is what would have found AQ5 without
being told where to look.

It did not retain the instrument itself. That is a deliberate omission and the one this pass is least
comfortable with: section 1.1 of the plan is about instruments being rebuilt every cycle and thrown
away, AO3 fixed that for the probes, and this pass has just built a second instrument and kept only its
output. **Retaining the trace as a gate is the ranked next work**, and it is what would make "every
check in these files runs" a measure rather than a thing a pass discovers by hand.

It leaves **condition 4 open**, and the count has stopped falling: three, six, three, two, **five**.
That is not a regression in the work. Four of the five were found by an instrument that had never been
pointed at these files, and the instrument found them in one run — the count measures what the previous
passes could not see, not what they let in. The one thing the next pass inherits is narrower and
sharper than what this one did: **run the trace, then hunt the negative assertion whose extent nothing
declares.**

## Where this family is dispositioned

AQ is raised against the verification work rather than against the design, as AM, AN, AO and AP were,
so under the owner ruling of 2026-08-20 its disposition lives in
[section 2h of the verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md)
and the [provenance table](./README.md#finding-family-provenance) declares it on both axes. No AQ
finding reaches a design artifact.
