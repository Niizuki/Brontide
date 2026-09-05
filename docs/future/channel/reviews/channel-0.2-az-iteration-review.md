# Channel 0.2 fourteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-fourteenth-pass-2026-09-05-9d615bf`

Reviewed work: the generated-vector instrument and `C4-P2`'s frame references, at `9d615bf`,
`Merge pull request #147 from Niizuki/channel-0.2-condition-4-thirteenth-pass`

Date: 2026-09-05

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **fourteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **second of the two consecutive clean passes** the 2026-09-04 ruling
requires before the closure cycle resumes.

**It is not clean. It records three findings against the package, all in the retained verification, so
the two-consecutive count resets and the thirteenth again stands alone.** None is in the design. All
three are in the instruments that report on the design. The first is the reason the thirteenth pass's
central claim about its own evidence was not true, and the third makes the design gate report a
spurious failure on any fresh checkout — which is to say, on CI, today.

## Section numbering

`AZ3` is the instrument this pass built rather than a finding. It is numbered in this sequence because
the plan's disposition rule routes an id, not a category, and a gap in the sequence is harder for the
next reader to reconcile than a numbered non-finding is to read. The findings are **AZ1**, **AZ2** and
**AZ4**.

## The frozen set, run first

**Zero.** 80 probes, the coverage measure, the owned-fact and design gates, and the 2,000-vector
generated run, all green before any of this pass's work existed. Eighth consecutive clean frozen set,
and strictly larger than the thirteenth's: it gained the `C4-P2` generated records that pass built and
quarantined.

That number is reported here with the caveat AZ1 below makes unavoidable: **the frozen set could not
have reported one whole class of failure**, and its green is that much narrower than it reads.

### AZ1 — the generated run discarded every "cannot be evaluated" report

An evaluator in `build/verify-channel-0.2-properties.ps1` returns two things: a verdict, and an
`Errors` collection through which a property says it could not be evaluated over a record at all —
a reference carrying no operand, or one resolving to no declared stimulus step. That distinction is
`C4-P2`'s AK6 machinery, and it exists because a property that could not read its operand is
**unevaluable**, not green.

The declared-input loop has always drained that collection. **The generated-vector loop never read
it.** It tested the verdict and nothing else, and the verdict beside a non-empty `Errors` is `green`,
because a record the property could not read produces no witness.

It was proven rather than argued. Pointing the generated `unseen` refusal's refused-frame reference at
an identity no declared step carries makes **every** first-conjunct record in the population
unresolvable. The run reported `0 red` over 25 vectors and **passed**. `C4-P2`'s first conjunct
asserted nothing on any of them and nothing said so.

This is the vacuous pass this whole instrument exists to detect, one level below where the twelfth
pass found it. Its consequence for the record is specific: the thirteenth pass's review states, under
*What this pass verified rather than believed*, that "an unresolvable one raises an error the run would
report, and the run is clean." **The run would not have reported it.** The thirteenth pass's green
verdict on `C4-P2` is, as far as this pass can tell, correct — its references do resolve — but the
sentence it offered as evidence for that green was not evidence, and a closure reviewer reading it
would have been entitled to say so.

Corrected: the generated loop raises each reported error against the vector, naming the property and
the record. The falsification above now fails the gate, and probe **AZ1-a** pins it.

### AZ2 — the generator gave arrival ordinals a uniqueness the design does not give them

The contract states the refused-frame reference carries "its **arrival ordinal** for that interaction
identity". The declared corpus counts it that way: per receiving endpoint, per session, per identity,
restarting at 1 in each. In `C4-two-sessions-one-identity` the two sessions' frames for one identity
are **both ordinal 1**, and that collision is the only reason the session operand is load-bearing
there — which is AF8's sentence and AK1's finding.

The generator used **one counter running across the whole vector**. Every generated ordinal was
therefore globally unique, and a reference publishing one was fully determined by it alone. Every
other field the reference published was redundant **by construction**.

That is not a small distortion of the population. It is the reason the instrument this pass was sent
to build could not have worked: with globally unique ordinals, **fourteen of the fifteen droppable
reference fields moved no verdict**, and AK1 and AK5 — the two findings the whole increment is about —
could not reproduce on generated input at any vector count. The generator was publishing a stronger
guarantee than the design makes, and the effect was to make the design's own operands look redundant.

Corrected: the ordinal is counted per receiving endpoint, session and interaction identity, as the
contract defines it and the declared corpus writes it. Probe **AZ2-a** pins it by reverting to one
counter and requiring the operands that stop discriminating to be reported.

Two further shapes were added with it, because one identity per session cannot carry what the retained
mutations are about — an identity **reused across sessions**, and a request arriving **between two
controls naming one identity**. Both are conforming, and both are shapes the declared corpus already
carries by hand.

### AZ3 — the instrument this pass built, and what it reports

The dropped-field sweep removes each field from each of `C4-P2`'s three frame references on every
swept generated vector, evaluates the property, and requires the outcome the sweep declares. A
reference resolves to every step matching the fields it **publishes**, so dropping one widens the
candidate set — and that widening is what the nine retained operand mutations assert by hand today.

Eighteen droppings, three outcome classes, all three occurring:

| Dropped from the reference | Outcome | What it reproduces |
| --- | --- | --- |
| the whole `refusedFrame`, `settlingFrame` or `terminalFrame` | **unevaluable**, every vector | the conjunct has no operand; AK6's machinery |
| `refusedFrame.arrivalOrdinal` | **red** through conjunct 1, every vector | **AK5**, and Y4's argument on that operand |
| `refusedFrame.interactionIdentity` | **red** through conjunct 1, every vector | the identity operand of both the request set and the membership test |
| `refusedFrame.session` | **red** through conjunct 1, every multi-session vector | **AK1** on conjunct 1 — AF8's two-session reuse |
| `settlingFrame.session`, `terminalFrame.session` | **red** through conjunct 2, every multi-session vector | **AK1**'s class on conjunct 2 |
| the remaining seven fields | **green** | recorded, not raised |

Eight of the eighteen are load-bearing. The three declared to discriminate only between sessions are
**required to be inert** on a single-session vector rather than merely allowed to be: a field that
moved a verdict where the scope says it cannot would mean the scope is what is wrong. Every red is
required to arrive through its **named conjunct**, for the reason the declared harness gives.

The seven greens are recorded as a limit of the population and **not** as evidence the fields are
redundant. Closure review 16 recorded their declared counterparts the same way, and adding an operand
is strictly more precise than inferring one.

**A correction to this pass's own brief.** The plan expected a dropped *field* to be able to leave a
reference "resolving to no step", and it cannot: dropping a filter only ever widens a candidate set,
never empties one. The unevaluable class is reachable only by dropping a whole reference, which is what
the sweep does. That is a correction to the brief rather than a finding.

### AZ4 — a guard whose regex ended in a newline typed into its own source

The AW1 guard requires a pass that parses no finding heading to **say** it found nothing, in a declared
form, so that a heading pattern which has quietly stopped matching still fails. It matched that
declaration as a whole line, and the line terminator in its pattern was **a literal newline typed into
the gate's own source file**.

`.gitattributes` declares `*.ps1 text eol=crlf`. A fresh checkout therefore gives the gate CRLF, the
pattern becomes `\r\n?$`, and it cannot match a line in an LF-only markdown file — which every
artifact here is, by the same `.gitattributes`. The guard then fires on **every review that
legitimately declares no findings**, which is the one outcome condition 4 asks a pass to produce.

It is invisible in this working tree only by accident: these gate files predate the renormalization
`.gitattributes` describes and are still LF on this disk, so the pattern is `\n?$` here and matches. It
was reproduced by running a CRLF copy of the gate — what `actions/checkout` produces — against the
unchanged review corpus, and it reported the thirteenth pass's review as having neither findings nor
the declaration.

This is AM4's lesson on a second axis: a verifier that reads text reads through a line-ending
convention nobody chose. Corrected by writing the terminator as the escape sequence `\r?$`, which
matches under both conventions and no longer depends on the gate's own bytes. The rest of the gates
were searched for the same shape and carry none.

## Findings

Three, **AZ1**, **AZ2** and **AZ4**, all against the retained verification and none against the
design. All three are corrected in this pass.

The sweep itself found **nothing in the package**: every one of the eighteen droppings behaved as
declared on every swept vector, and the 2,000-vector generated run remains 0 red.

Two defects were found in the **instruments** before they were believed, and are recorded here as what
falsification is for rather than as findings. Both were mutations written to prove a check fires, and
both were wrong rather than the check:

- The first attempt to revert AZ2 keyed the counter globally but left it reset per session, so ordinals
  still collided across sessions, the session operand stayed load-bearing, and the run was clean. A
  pass that had stopped at "no failure" would have concluded the sweep could not detect the very defect
  it had just been built around.
- The same shape recurred in the second attempt, which changed only the counter's lifetime and not its
  key. Reverting AZ2 needs **both** edits, which is what probe AZ2-a carries.

This is the thirteenth pass's own lesson arriving twice more in one pass, and it is the argument for
keeping every one of these mutations in the probe corpus rather than in a transcript.

## What this pass verified rather than believed

- **The frozen set was run first**, and is strictly larger than the thirteenth's.
- **AZ4 was reproduced rather than reasoned about.** The mechanism was worked out by reading, and then
  a CRLF copy of the gate was run against the unchanged corpus to see it fire. Two earlier conclusions
  in this pass had already been wrong on exactly that step.
- **Every declared outcome in the sweep was driven wrong and reported.** Twelve falsifications: each
  declared verdict flipped, the multi-session scope widened, a named conjunct swapped, the restore
  broken, each generator shape removed, and two of `Resolve-FrameReference`'s own filters disabled.
  Eleven reported on the first attempt and the twelfth after its mutation was corrected.
- **AZ1's fix fails on the input that used to pass silently**, which is the only evidence that
  distinguishes a check that drains a collection from one that does not.
- **The rewritten `Resolve-FrameReference` kept its meaning.** It is one pass over the steps instead of
  five pipelines, and the nine declared operand mutations, the 55-vector declared corpus and the
  sweep's eight load-bearing droppings all still report exactly what they did before it.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs.

## What remains outside the pass

**The seven inert droppings are inert because the population is one frame shape per session.** They
are the fields the declared corpus also records as moving no verdict, and nothing here promotes that
to a claim about the design.

**The generator produces conforming vectors only.** The mutation direction is still applied by hand and
discarded, in this pass as in the three before it.

**`C4-P1`'s in-flight bound remains scoped to one named profile**, and the direction scope for a
profile in which both endpoints initiate is still undecided — a limit the completeness review states
rather than one this pass changed.

**A cost was moved rather than removed.** The sweep costs eighteen further `C4-P2` evaluations per
vector it covers, and the guard corpus re-runs this gate once per probe, so it has its own count:
forty per commit and five hundred on the deep run. Pinning it to the generated count would have
multiplied what the probe corpus costs rather than what one run costs, which is the AT7 ruling's
distinction between absorbing a cost and naming it.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, **three**.

## Where this family is dispositioned

**AZ1**, **AZ2** and **AZ4** are corrections to the verification instruments, not to the design, so
under the 2026-08-20 ruling they belong in the verification foundation plan's own record and not in the
completeness review's disposition index. The plan's section 2q carries them, and this document is the
pass's evidence.
