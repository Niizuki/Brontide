# Channel 0.2 twenty-first W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twenty-first-pass-2026-09-15-06f9547`

Reviewed work: the read-provenance census over the polarity it could not see -- every field of every
input a property is declared red on, poisoned one at a time, against which reader performed the read
-- and the entry points that record a pass, at `06f9547`, `Merge pull request #154 from
Niizuki/claude/next-item-implementation-490729`; raised and dispositioned the BG1-BG2 findings this
document records

Date: 2026-09-15

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twenty-first** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**Under that ruling's two populations it is clean, and it records two findings all the same.** The
frozen set reported nothing at `06f9547` -- the first clean frozen set since the twentieth broke a
streak of thirteen. The instrument this pass built, the read-provenance census walked over the
red-expected inputs, found **nothing in the package** on its first run: 48 (property, mutation) pairs,
503 further reads observed, every one through a sanctioned reader. **BG1** and **BG2** were both found
by reading, which is the population the 2026-09-04 ruling does not count and **AW2** asked the owner
about ten passes ago; BG1 is against a frozen instrument and BG2 against an entry point, and neither
is in the design. Whether a pass that records them is the first of the two owed is therefore AW2's
question and not this document's to settle, and it is stated both ways in the last section rather
than decided here.

## Section numbering

**BG1** and **BG2** carry numbers because the corrections cite them. Neither is what the frozen set
reported about the package, and neither is what the instrument this pass built reported on its first
run; both were found by reading. BG2's correction includes a guard, and that guard's first run is
recorded under BG2 because it reports the same defect a second and third time rather than a new one.

## The frozen set, run first

**One.** In a short-path clone at `06f9547`, before any of this pass's work existed: the text and
link guards, the design gate, the owned-fact gate, the return-channel census, the properties gate at
its default count and at the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors
at 0 red, AZ3's dropped-field sweep over 500 of them with all eight load-bearing droppings reporting
where declared, the read-provenance census over the green polarity at 1,017 of 3,683 poisoned fields
read and 0 raw, the field-readership census at 0 read by nothing, the closed-vocabulary census over
both populations, the declaration-polarity and declaration-citation checks -- the 130-probe corpus at
130 of 130, and the coverage measure over four gates. All exit 0. The streak of clean frozen sets
restarts at one.

## The instrument

The read-provenance census poisons one field of one input at a time -- the record keeps the field and
the field has no value -- runs the evaluator, and takes the reader that performed the read off the
call stack. From BB1 to BF it walked green-expected inputs only, on the reasoning that a poisoned
field leaving a declared mutation red says nothing about whether the record could be read. That is
true of the outcome and was never the question: the census classifies by **reader**, and a raw read
is a raw read on whichever polarity reaches it. The field-readership census had measured the cost at
ten fields read only where a property is declared red, and the `BB1-c` probe had recorded, as an
expect-pass, that a raw read in `S1`'s own witness -- reached only on `S1-illegal-transition-accepted`
-- was invisible.

The census now walks the named mutations too. Each red-expected input has its own baseline, which must
be red and, where the mutation declares a conjunct, red through it: what a poisoned field did is
measured as a difference from that baseline, so a baseline on the wrong polarity or reached the wrong
way would attribute the difference to the wrong thing. The classes a raw read can fall into gain one
the green polarity cannot produce: **moved the verdict to green**, which is an unreadable record
passing as a conforming one -- the failure BB1 was raised to end, reached from the side BB1 could not
see. The cap `-CensusPairs` counts per polarity, so the coverage measure's one-pair run still walks
both. `BB1-c` expects the failure it used to record the absence of, and four probes pin the rest.

**On its first run over the package as found it reported nothing.** 1,520 of 5,553 poisoned fields
were read by an evaluator over 89 green-expected and 48 red-expected pairs, all 1,520 through a
sanctioned reader, against 1,017 of 3,683 over the green polarity alone. The ten fields read only on a
red-expected input are read through a sanctioned reader on every such input, and no property is
silent on any mutation it declares.

**The clean report was falsified before it was believed.** `BB1-c`'s mutation, restored, is reported
on `S1-illegal-transition-accepted` at both `S1` and the `C2-P1` that delegates to it, as a raw read
that left the property red and reported nothing -- the finding BB1's limit said it could not make.
`I4` reading a refusal's `stage` raw is reported on both of `I4`'s mutations as having **moved the
verdict to green**; no green-expected input of `I4` carries an interaction refusal, so that read is on
a path the frozen census could not reach at all, which is exactly the class the pass was sent for. A
mutation declared against the wrong conjunct, and a mutation vector edited into conformance, are each
refused at the census's own baseline as they are at the declared loop's.

**One class was left out rather than half-built.** A raw read on the red polarity could in principle
move the red between two conjuncts of one property. No declared vector carries records for two clauses
of one property, so no one-line edit provokes it, and a classification branch nothing can reach is a
hypothesis rather than a check. A red that stays red is one class, whichever obligation it now arrives
through; `C5-P1` reading `boundsChecked` raw moves the red between two obligations of one conjunct on
`C5-positional-shape-unchecked` and is reported as having left the property red, which is true.

### BG1 -- the frozen census's baseline recorded a declared input's absence as `undeclared`

BC1 made the two meaning-carrying readers record, for each absence they observe, the declared verdict
of the input being evaluated, and its comment names the three dispatches with no declared verdict -- a
generated vector, an operand mutation, a dropped field -- whose absences are recorded as `undeclared`
rather than guessed at. The census's baseline is none of those: it evaluates a declared input whose
verdict the declaration file states, and it ran with no expectation in scope, so `refusal`'s
`Read-Optional` carried `green/red/undeclared` at the head where the declared loop had recorded
`green/red`. The readership census next door sets the expectation for exactly this reason and says so.

No verdict moves on it: the declared loop records the true polarity first, and BB5 and BC1 ask only
whether a declared polarity is present. What moves is the failure message, which would have listed
`undeclared` beside the polarities as though a declared input had none. Pinned by dumping both tables
after a run at the head and after the correction: `undeclared` is present at the head and absent
after, at `-GeneratedCount 0`, so the baseline was its only source in a declared-corpus run. The
baseline now runs under the input's declared polarity on both walks, and the poisoned runs suppress
those readers' records as they did.

### BG2 -- the review policy's roster understated three families, and nothing read it

The next-work section of the review policy lists every condition-4 pass with what it raised --
`[twentieth](./channel-0.2-bf-iteration-review.md) (**BF1**-**BF11**)` -- and the twentieth raised
twelve. The commit that recorded that pass updated the future index, the Channel index, the plan and
the reviews README's own retained-record roster, and left this list, which no guard reads. That is
AX1's class a fourth time: the entry points that go stale are the ones only prose carries.

The guard written to pin the class reads each listed entry back against the finding headings of the
review it links to, and on its first run it reported two more. The first pass is listed as
**AM1**-**AM3** and its review carries five corrected findings: **AM4** and **AM5** were raised inside
that pass, against its own correction and by CI, after every surface stating the family's size had
been written -- and the Channel index, the future index and the plan's section 2d and section 3 tally
all still said three, as did the AM review's own header, which is a retained record and is left as it
stands. The fourteenth is listed as **AZ1**-**AZ2** with **AZ4** left off, where the Channel index
had it right. All three roster entries and the four AM surfaces are corrected.

**The guard's rule, and its limit.** Every id the roster states must have a heading, and the largest
it states must be the largest the review carries. An omission strictly inside a range is not caught,
deliberately: `AZ3` is a numbered non-finding the fourteenth review declares as such, so the roster
lists `AZ1`-`AZ2`, `AZ4` and a set-equality rule would fail the truth. The guard is scoped to the
roster because its form is precise; a check over every `**XX1**-**XXn**` in the three narratives
reports two legitimate sub-ranges -- a commit correcting `AT1`-`AT3`, and `BA1`-`BA4` as the subset a
pass found in the package -- so the Channel index's range list and the future index's per-pass
sentences state the same fact and are read for the family token by the AJ2 check, not for the range.
The plan's section 3 tally is prose too, and it is corrected by hand for AM and not guarded. The guard
is quarantined and joins the frozen set at the twenty-second pass.

## What was corrected on contact

The `-CensusPairs` parameter's comment and the capped summary line say the cap is per polarity. The
census header's first stated limit says what it measures now, and that a read on a path no declared
input reaches is still invisible, with the sixteenth pass's note on the generated population standing.
The `BB1-c` probe's claim says what it is now: the limit closed, and the probe that records its
closing.

## Findings

| id | where | what |
| --- | --- | --- |
| **BG1** | a frozen instrument, by reading | the read-provenance census's baseline ran a declared input with no declared expectation in scope, so the meaning-carrying readers recorded its absences as `undeclared` -- the value BC1 reserves for dispatches that have no declared verdict; corrected at both walks |
| **BG2** | an entry point, by reading | the review policy's next-work roster listed the twentieth pass as `BF1`-`BF11` against twelve headings, the first as `AM1`-`AM3` against five, and the fourteenth as `AZ1`-`AZ2` without `AZ4`; a guard reads every entry back against its review, and the AM size is corrected on four surfaces |

## What this pass verified rather than believed

- **The frozen set was run at `06f9547` before any of this work existed**, in a short-path clone, and
  everything reported above was read from that run's output. The deep properties run took 585
  seconds; the probe corpus 1,720 over 22 workers and the coverage measure 1,656, both on a machine
  also running this pass's own gates. A coverage run over the instrument commit in the working tree
  reported 139 constructs of the design gate unreached, every one below the line where this pass
  had inserted sixty lines into that gate while the measure was tracing it -- the trace's line numbers
  were the old file's -- and none in the properties gate; the measure's own header says an
  uncommitted gate is outside its contract, so that run is discarded rather than read, and the run
  over the final head below is the one that counts.
- **The instrument was made to fail for each of its claimed reasons before its verdict was believed**,
  and five probes keep the corpus honest about it: `BB1-c`, flipped from expect-pass to expect-fail
  on the raw witness read that was its subject; `BG-a` and `BG-b`, one raw read that no green-expected
  input reaches, reported on the red polarity and classified as having moved the verdict to green;
  `BG-c`, a mutation declared against the wrong conjunct, refused at the census's baseline; and
  `BG-d`, a mutation vector made conforming, refused there too. Each was run alone and seen to return
  the verdict it owes before the corpus was run over the branch. The corpus goes from 130 to 134.
- **BG1 was pinned by observation, not by reasoning.** The declaration tables were dumped after a run
  of the head's gate and of the corrected one; the value was there and then was not.
- **BG2's guard was seen red for all three stated reasons before the roster was corrected**, and it
  reported AM and AZ before anyone had read those entries. The AM and AZ headings were then read to
  confirm what it said: five corrected findings, and a declared non-finding at `AZ3`.
- **The frozen instruments were run over the branch as it was built.** The return-channel census
  passes over the extended gate with the same twenty consumers. **The whole set was then run over
  `490fd81`, the head that records this pass, in the same clone: the text, link, design, owned-fact
  and return-channel gates, the properties gate at its default count, the corpus at 134 of 134 over
  22 workers in 1,433 seconds, the coverage measure over four gates in 1,275 seconds with the same
  41 condition and 6 operand exemptions it declared before this pass -- so nothing the pass added
  needed one -- and the deep run at 52,000 evaluations and 0 red with the sweep green over 500, all
  exit 0.** The one commit above that head changes this paragraph and one arithmetic sentence in the
  plan's section 3, and the text, link, design, owned-fact and properties gates were re-run over it.

## What remains outside the pass

**A read is still a dereference, not an operand.** The dropped-field sweep asks whether a value can
move a verdict over frame references and nowhere else, and this pass did not generalise it.

**The census still poisons the declared corpus only.** A raw read on a path only a generated vector
reaches is invisible to both walks, and the sixteenth pass's trade -- a thousand times the cost for a
population a thousand times larger -- stands unmade.

**The conjunct-moving class is unpinned because it is unprovokable.** A declared vector carrying
records for two clauses of one property would make it a check; that is a vector, not a guard.

**Three narratives state finding ranges the guard does not read.** The Channel index's list, the
future index's per-pass sentences and the plan's tally were corrected for AM by hand. A guard over
them needs a rule that tells a whole-family range from a legitimate sub-range, and the two sub-ranges
named above are why one was not written here.

**The profile record** behind BF2 and BF9 is still the owner's.

**Cost.** The red walk adds 48 baselines and 1,870 poisoned evaluations to the properties gate at
`-GeneratedCount 0`: 21.2 seconds at the head against 28.6 on the branch, both measured on a machine
running two coverage measures at the time, so about seven seconds. The corpus gains four probes at
that count.

**Whether this pass is the first of the two owed is AW2's question.** Read one way, both of the
2026-09-04 ruling's populations are zero here: the frozen set reported nothing and the new instrument
found nothing in the package, and BG1 and BG2 are outside both, as BF11 was. Read the other way, the
roster guard is an instrument built during the pass whose first run found something in the package,
and the ruling's second test is not met. The eleventh pass put that question to the owner and it has
not been answered; this record states it and does not decide it. The closure review remains on hold.
The finding count by condition-4 pass is now five, six, three, two, five, one, seven, seven, five,
three, one, three, zero, three, seven, seven, three, three, five, twelve and **two**, the first of
those corrected under BG2 from the three it had said for twenty passes.

## Where this family is dispositioned

Neither finding reaches a design artifact: BG1 is in the properties gate and BG2 in the design gate
and the review policy, the future index, the Channel index and the plan. The family is classified
`verification` and is dispositioned in the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md), whose
section 2x carries the pass; this document is its evidence.
