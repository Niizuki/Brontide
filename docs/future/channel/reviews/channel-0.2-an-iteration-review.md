# Channel 0.2 second W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-second-pass-2026-08-20-0e43a69`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), and W3 (the status blocks and
the Channel index rows) — at `0e43a69`, `Merge pull request #132`; raised and dispositioned the
AN1-AN6 findings this document records

Date: 2026-08-20

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the **second** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, run after the [first](./channel-0.2-am-iteration-review.md) raised AM1-AM5 and corrected them.
**Condition 4 is not met by it either.** That condition is an author-side pass that *finds nothing it
can fix*, and this one found six things.

## Method

The plan scopes this pass to the verification work rather than to the design the sixteen closure
reviews have read, and the review policy's next-work paragraph says this pass should **re-run the
probes the first pass recorded rather than trust them**. Both were done, in this order:

1. **Re-run every probe the AM review records**, as its own closing paragraph asks. Twenty-one
   mutations across the three gates, listed under *what this pass verified* below.
2. **Recompute every number** these sections state, against the repository rather than against the
   previous pass's report.
3. **Re-derive every claim one document makes about another**, including the claims the AM
   corrections make about the checks they changed.
4. **Probe every guard the AM pass added or rewrote**, and every guard W1, W2 and W3 added that the AM
   pass probed, by breaking it deliberately.

Question 1 reproduced every AM result and found nothing. Questions 2 and 4 found two guard defects,
question 3 found four stale facts, and the guard written for AN5 found a fourth surface of its own
subject while it was being tested.

**The pattern across all six is one sentence: the AM pass corrected what it measured and did not ask
what else states the same thing.** That is the family this programme has recorded ten times, arriving
in the pass whose method was to recompute every number.

## Findings

### AN1 — the status-block pointer was never checked to resolve, and the check says it is — corrected

W3's whole claim is that a status block may carry no history because it carries a pointer. The check
that guards it asks four questions, and its own comment states the second: *"The pointer must RESOLVE,
because a link to a section that does not exist is worse than the history it replaced -- the reader is
told the record is elsewhere and finds nothing."* The plan's section 2b repeats it: *"the link resolves
to a section"*.

It did not ask that. It looked the section up **by the artifact's file name**, matching the `.md` link
inside each section body, and never read the anchor the status block actually carries. Three probes at
the parent commit, all green where two should have been red:

| Probe | Before | After |
| --- | --- | --- |
| A status block's anchor changed to `#session-state-machine-that-does-not-exist` | **green** | red |
| The index's `## Session state machine` heading renamed, leaving every pointer to it stale | **green** | red |
| That section deleted outright | red | red |

The third is the probe the AM pass recorded — *"a pointer to a deleted index section"* — and it is the
one instance of the class the by-name lookup happens to catch, because deleting the section removes the
link the lookup keys on. Renaming a heading is the instance a maintainer actually produces, and the
disposition index gains a section every time a family is dispositioned.

Nothing else in the repository covered it: `build/verify-doc-links.ps1` splits the fragment off the
target and checks only that the file exists, so a `#anchor` naming no heading was invisible
repository-wide.

**Corrected in two places, at two scopes.** `build/verify-doc-links.ps1` now resolves every fragment
in every Markdown link against the headings of the document it points at, duplicate-heading suffixes
included, across every Markdown document in the repository; the gate reports the counts and all of
them resolve. They are not restated here, for the reason AN5 records: this document is retained and
those two numbers move whenever a link is added. And the design verifier
resolves the pointer each status block and each Channel index row actually carries, against the
disposition index's own headings, and requires a status block's pointer to land on the section about
**that artifact** — which a link checker cannot know and which the fourth probe showed is a real gap:
pointing the session machine's block at the interaction machine's section resolves perfectly well.

### AN2 — the review-target pin compares eight of the nine design artifacts — corrected

**AM5** rewrote this check so that a pin is valid when the design artifacts at the pinned commit hash
identically to the tree a reviewer reads now. The artifacts it compares are a list written out in the
verifier, and that list holds **eight**. The ninth is the redesign plan — item 3 of
[the review policy's own required review scope](./README.md#required-review-scope), the artifact that
carries the four resolved owner rulings and the 2026-08-15 closure standard.

So a commit that changes only the redesign plan moves material every closure review reads and leaves
the pin pointing at a commit that no longer has it. Probed by committing a paragraph to that artifact
alone and running the gate: **passed**. Five commits in this history changed that artifact and nothing
else, one of them `66729b0`, *"resolve design owner rulings"*.

This is U6 — a reviewer sent at artifacts that have already moved — inside the check written to close
U6, for the reason X6 and AM5 were both about: a second hand-written list of one class.

**Corrected** by deriving the pathspec from `$artifactNames`, the list the rest of the file already
uses, minus the two READMEs. One list, so a tenth design artifact joins both questions at once. The
plan-only probe is red under the fix.

### AN3 — the third measure's history is prose, and none of it reconciles — corrected

Section 4 carries five measures. **AM2** and **AM3** recomputed the two that stated a number and a
commit, and recorded the split as this plan's own thesis: *"of section 4's five measures, the one the
properties gate recomputes was correct, the two owned facts' counts were correct, and both measures
left to prose were wrong."*

The fifth measure — this verifier's own length — has a recomputed *current* value and a **history left
entirely to prose**, and that half was not examined. It read: *"It fell at each step through the AM
correction — W1 took 169 lines out with the frame-reference registry, W3 took 32 more with the
index-row freshness checks, and the AL2 record-keyed sweep took 16 more — and the AM pass put 182
back."* Measured:

| Commit | What it did | Lines | Delta |
| --- | --- | --- | --- |
| `6c7715a` | the work begins | 2,322 | — |
| `365bbc0` | W3, status blocks | 2,377 | **+55** |
| `2684ec7` | W1, frame references, registry deleted | 2,257 | −120 |
| `72fecde` | W3, index rows | 2,263 | **+6** |
| `46b7c85` | W1, `unseen` record, AL2 sweep out | 2,247 | −16 |
| `0f7858c` | AM1-AM3 | 2,356 | +109 |
| `6a6c76d` | disposition routing | 2,441 | +85 |
| `c5fe9ee` | AM5, the pin | 2,491 | +50 |

It **rose** at two of those steps, so "fell at each step" is false. Of the four deltas named, only the
AL2 one (−16) is a figure any reading produces: the registry commit deleted 160 lines from this file
for a net −120 and no reading gives 169; the index-row commit deleted 21 for a net +6 and none gives
32; and the AM commits add +159 net or +174 gross, or +244 with the disposition-routing commit, and
none gives 182. The net movement over the whole work was **+169**, which is the one place that number
is produced — by a different subtraction than the sentence claims.

**Corrected** to the table above, stated in the plan in a form the design verifier recomputes commit
by commit, alongside the plain statement that the measure has risen overall. That is AM2's remedy
applied to the measure AM2 stood beside.

**AM4's lesson arrived again while the check was being written, and is why the reader is now named.**
The recomputation disagreed with `wc -l` at exactly one commit — 2,356 against 2,354 at `0f7858c` —
because the file carried two stray carriage returns there, and PowerShell's line reader treats a lone
CR as a break while `wc -l` counts newlines. A verifier that reads history reads through a decoder
nobody chose, and it also counts through a *reader* nobody chose. The plan now names the reader and
says where the two disagree.

### AN4 — the publication-site count says twenty in four places and the repository holds 21 — corrected

The plan's section 2c states the count twice — *"rendered into all twenty publication sites"* and
*"rewrites all twenty sites in five artifacts"* — in the present tense, describing how W1 works. Both
were exactly right at `2684ec7`, when the three frame references were the owned facts. Four paragraphs
further down, the same section records the `unseen` refusal record becoming the fourth owned fact,
which is what makes the number 21 across 6 artifacts. Section 4's copy was corrected to 21. Section
2c's two were not, nor was the disposition index's, nor were three comments in
`build/verify-channel-0.2-facts.ps1` — *"the twenty registered sites"*, *"a TWENTY-FIRST surface"*,
*"one edit that rewrites twenty sites at once"*.

No reading of the repository gives twenty now: 21 fenced sites across 6 artifacts, or 16 across 5 for
the three frame references alone.

**Corrected**, and the count moved to where it is computed: the facts gate counts the sites and the
publishing artifacts and requires the plan's sentence to agree with both. Probed by editing each half
of the sentence. The gate's own summary line was wrong in the same direction and is fixed with it — it
attributed the 21 publications to *"14 artifacts"*, which is the number of files it opens.

### AN5 — the corrected measure was still stated in two other maintained records — corrected

**AM2** found the status-block line total wrong, corrected it from 289 to 265, and named its surfaces:
*"Section 2b and section 4 both stated that the nine blocks held 289 lines."* There were four. The
**disposition index** — the file W3 created, and the file every status block sends its reader to —
opened with *"Nine status blocks had reached 289 lines between them"*, and said so for three commits
after the correction. The **future-work index** carried the number too, correctly at 265, which is a
second copy of a measure and stale the moment the next status block is edited.

That the pass whose stated method was *"recompute every number"* left the same number wrong two files
away is the finding, not the number itself: recomputing a value in the document that owns it says
nothing about the documents that repeat it.

**Corrected by removal rather than by a second guard**, which is W1's remedy and this plan's own
preference: both records now cite the measure the plan owns. The design verifier sweeps the maintained
records — the two indexes, the review policy, the disposition index — for any restatement of a
status-block line total, and retained attestations and iteration reviews are excluded because the
policy makes them immutable and they legitimately record the readings they rejected, 289 among them.
**The sweep found the future-work index's copy on its first run; this pass had found only the
disposition index by reading.**

### AN6 — the review policy still described the pin mechanism AM5 removed — corrected

The pin clause told a reviewer: *"The design verifier now compares this sentence against the most
recent commit that changed a design artifact and against that commit's own date."* After AM5 it does
neither. It resolves the pinned subject to a commit and compares blob hashes, and the date it checks is
the **pinned** commit's, both changed for the same reason: *"which commit last changed a design
artifact"* has two different answers, one on the merge commit a `pull_request` build checks out and one
on the linear branch, and AM5 is the record of that costing a red CI run.

So the policy described, as the guarantee a reviewer relies on, precisely the question AM5 established
the gate must not ask. **Corrected**, and this one has no check behind it: nothing here can compare
prose to what code does. What can be said is that it was found by the eleventh review's method note —
for every claim a correction makes about an artifact it did not edit, open that artifact — and that
AM5 edited the verifier and the plan's section 2d and not this sentence.

## What this pass verified rather than believed

Recorded because a pass that reports only what it broke gives the next reviewer no idea what was
checked. **Every probe the AM review records was re-run and every one reproduced.**

- **The AM1 status-region bound.** The AL1 disposition paragraph restored beneath a block fails; the
  same history under an undeclared `Correction history:` label fails; a real `Designed for:` paragraph
  passes.
- **The W3 checks.** A six-line block, a removed pointer, a deleted index section, an index row that
  loses its pointer, and an index row that grows a paragraph each fail.
- **The AM2/AM3 measure checks.** A wrong now-value, a wrong then-value and the stated form removed
  each fail, for both measures, and the design-verifier line measure fails when it disagrees with the
  file.
- **The hold checks.** Editing the closure-cycle state to `resumed`, and editing the attestation count
  upward, each fail.
- **The W2 rules.** Removing `C4-P2`'s named mutation fails; removing `S3`'s two-session required-green
  member fails.
- **The W1 machinery.** A hand-corrected fenced site, a deleted fence, a field added to a nested fact,
  a class member losing a field name, and the record stated in prose outside every fence each fail.
- **The W1 sweep on the parent commit.** Run against `46b7c85~1` with the current declaration, it fires
  on exactly three surfaces — the interaction machine's `unseen` row and both grid cells — as section
  2c claims, and on nothing else.
- **Every W2 number is exact**: 88 evaluations over 40 declared inputs for all twenty-six properties,
  55 over 29 for the fifteen condition 2 names, eleven inputs and nine operand mutations for `C4-P2`,
  three of those nine flipping a verdict and six not.
- **AM4's decode fix holds.** The index-row measure recomputes, independently of the verifier, to
  exactly the two figures section 4 of the plan states — including the historical half at `2684ec7`
  that the mis-decoding had put eight characters high.
- **The W3 move is verbatim and lost nothing.** All 152 moved sentences of the nine disposition-index
  sections appear verbatim in the artifacts' status regions at `365bbc0~1`; all 118 finding tokens from
  those regions and all 104 from the eleven pre-W3 index rows are present in the index.
- **The Channel index's own counts hold**: 16 retained attestations, five iteration reviews before
  this one, and the nine artifact rows between 94 and 106 characters each, summing with the two longer
  rows to the total the plan's measure states and the design verifier recomputes.

## What this pass did not do

It did not re-read the design. The plan scopes condition 4 to the verification work, and a pass that
re-reads C1-C12 in the working repository is neither an independent review nor a use of the one thing
an author-side pass is good for.

It did not examine the two measures it could not recompute — findings per cycle by class, and surfaces
per fact — beyond confirming the second against the fact declaration, which is where the AM pass also
stopped.

It leaves **condition 4 open**. Six findings is not "nothing it can fix". A third pass is what can
close it, and the standing instruction holds for it too: re-run the probes recorded here rather than
trust this document. Two things worth its attention first. Every finding here was in the *second*
place a fact was stated, never the first, so the question that pays is not "is this number right" but
"where else is this number" — asked mechanically, since the AN5 guard found a surface this pass's
reading had missed. And two of the six were guards whose comment stated a stronger question than the
code asked; reading each guard's comment as a claim to be tested against its code is a cheap pass
nobody here has run in full.

## Where this family is dispositioned

AN is raised against the verification work rather than against the design, as AM was, so under the
owner ruling of 2026-08-20 its disposition lives in
[section 2e of the verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md)
rather than in the completeness review's history, and the
[provenance table](./README.md#finding-family-provenance) declares it on both axes. No AN finding
reaches a design artifact, which is the backstop that ruling carries: every correction here is in a
verifier, in the plan, in the two indexes, in this review policy, or in the disposition index.
