# Channel 0.2 W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-w1-w3-iteration-2026-08-20-88f2447`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), and W3 (the status blocks and
the Channel index rows) — at `88f2447`, `Merge pull request #131`; raised and dispositioned the
AM1-AM3 findings this document records

Date: 2026-08-20

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it.** That condition is an author-side pass that *finds nothing it
can fix*, and this one found three things. A further pass, after these corrections, is what can meet
it.

## Method

The plan scopes this pass to the verification work itself rather than to the design the sixteen
closure reviews have read. Three questions, in this order, chosen because the programme's own failure
history says prose review does not find this class:

1. **Recompute every number.** Section 4 of the plan exists so the next decision is made on evidence.
   Every count it states, and every count in the W1, W2 and W3 sections and in the two indexes, was
   measured against the repository rather than read.
2. **Re-derive every claim a document makes about another document.** The eleventh review's second
   method note — for every claim a correction makes about an artifact it did not edit, open that
   artifact — applied to the claims W3 makes about what it moved.
3. **Probe every check by mutation.** Each guard added by W1, W2 and W3 was broken deliberately and
   the gate was required to name the defect. A guard that has only ever been green is asserting
   nothing, which is C12's own rule turned on the verifiers.

Question 3 is what found **AM1**, and it found it through question 2: testing the "moved verbatim"
claim required knowing where the moved text had lived, and the answer was *outside the boundary the
length bound measures*.

## Findings

### AM1 — the status-block length bound stops at a blank line, and the history it excludes sat one blank line below it — corrected

W3's acceptance is that every status block fits in five lines, and the check's own comment states why
the length half is the one that matters: "a block that keeps its pointer and grows a paragraph beneath
it is the status quo with a link added, which is how every previous correction to these blocks went."

The check read the block from `Status:` to the **first blank line**. A paragraph one blank line
beneath it is outside that boundary and passes. This is not hypothetical: at `5894aba` the session
state machine's AL1 disposition — *"This status block previously recorded that the AK pass had audited
`S1`-`S6`..."* — was line 12 of that artifact, one blank line below a status block the bound was
measuring. The probe was run against the parent commit of this correction and was **green**: a
paragraph of disposition history appended below a five-line block satisfied every check in the gate.

It is the shape this programme has recorded nine times — a guard whose scope the defect steps outside
of — arriving in the check written to retire that class.

**Corrected.** The bound is now over the **status region**: everything between `Status:` and the
artifact's first section heading. The block is that region's first paragraph and keeps the five-line
bound; every other paragraph in the region must be **declared front matter**, from a list the verifier
carries (`Designed for:`, `Contract owner:`, `Companion artifacts:`, and six more), with a labelled
list's own bullets permitted under it.

The direction of that rule is the point. A guard that recognised disposition history *by the words it
uses* could not see the instance that does not use them, which is AL1 and AL2 exactly; a permit list
fails on anything it does not recognise, so a new kind of front matter is declared once and visibly,
and a paragraph of narrative is a gate failure on the commit that writes it. Bounding the region this
way also subsumes the reason AH3 once existed as a second check over the redesign plan, whose title
heading sits above its status line.

Three probes: the AL1 paragraph restored beneath the block fails; the same history disguised as
`Correction history:` fails as an undeclared label; a real `Designed for:` line passes.

### AM2 — the status-block line measure states a number no reading of the repository produces — corrected

Section 2b and section 4 both stated that the nine blocks "held **289 lines**". They did not, under any
reading this pass could construct:

| Reading | Value at `9ce01a0` |
| --- | --- |
| The check's own reader — `Status:` to the blank line, blank lines excluded | **265** |
| Read to the first section heading instead | 316 |
| The nine blocks plus the two README blocks | 314 |
| Lines the W3 commit deleted from those nine artifacts | 283 |
| Non-blank body lines of the disposition index's sections | 328 |

**Corrected** to 265, with the reader and the commit stated so a reader can re-derive it, and both
halves are now recomputed by the design verifier.

### AM3 — the Channel index row measure states a number that was never true at any commit — corrected

Section 4 stated "8,746 before W3 and 1,208 after". The before half is exact — 8,746 characters across
the eleven per-artifact state cells at `2684ec7`, averaging 795, as section 2b says. The after half is
**1,306**, both at `72fecde`, the commit that produced it, and at the parent of this correction. The
gap is 98 characters, about the size of one of the nine artifact rows.

Registering this review moved it again: naming the AM family in the Design reviews row added four
characters, and the new check failed the corrected figure on the same commit that wrote it. The
measure reads **1,310** now, and that exchange is the check working rather than a defect in it.

The two halves of one measure were produced by different methods, and the mechanical one is the one
that is right. That is this plan's own thesis, arriving in the plan: of section 4's five measures, the
one the properties gate recomputes was correct, the two owned facts' counts were correct, and **both
measures left to prose were wrong**.

**Corrected**, and stated as 1,310. Both halves of both measures are now recomputed by the design verifier from
the repository, historical half included, and the measure must be stated in a form that names its
commit. Five probes: a wrong now-value, a wrong then-value, and the stated form removed each fail, for
both measures. The then-value probe restores `289` and the gate names it.

## What this pass verified rather than believed

Recorded because a pass that reports only what it broke gives the next reviewer no idea what was
actually checked.

- **The verbatim-move claim holds.** Every sentence of all nine disposition-index sections at
  `365bbc0` appears verbatim in that artifact's status region at the parent commit. The first attempt
  said otherwise for two artifacts; the boundary was wrong, not the claim, and finding out which is
  what exposed AM1.
- **Nothing was lost in either move.** All 118 finding tokens in the nine pre-W3 status blocks, and
  all 104 in the eleven pre-W3 Channel index rows, are present in the disposition index.
- **Every W2 number is exact**: 55 evaluations over 29 declared inputs for the fifteen properties
  condition 2 names, 88 over 40 for all twenty-six, eleven inputs and nine operand mutations for
  `C4-P2`.
- **The W3 checks fire.** A six-line block, a removed pointer, a pointer to a deleted index section,
  an index row that loses its pointer, and an index row that grows a paragraph each fail the gate.
- **The W2 rules fire in both directions.** Removing a property's named mutation and removing the
  two-session required-green member both fail — and by a stronger mechanism than the one probed for:
  the vector declares its membership and the property owes every member an expectation.
- **The W1 sweep fires** on the three surfaces that stated the `unseen` refusal record in prose, and
  the owned-fact machinery catches a hand-corrected site, a deleted fence, and a field added to a
  nested fact.

## What this pass did not do

It did not re-read the design. The plan scopes condition 4 to the verification work, and a pass that
re-reads C1-C12 in the working repository is neither an independent review nor a use of the one thing
an author-side pass is good for.

It did not examine the two measures it could not recompute — findings per cycle by class, and surfaces
per fact — beyond confirming the second against the fact declaration.

It leaves **condition 4 open**. Three findings is not "nothing it can fix". The pass that runs after
these corrections is the one that can close it, and it should begin by re-running the probes recorded
here rather than by trusting this document.
