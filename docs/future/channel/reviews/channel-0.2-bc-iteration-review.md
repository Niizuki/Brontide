# Channel 0.2 seventeenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-seventeenth-pass-2026-09-10-ceeddc3`

Reviewed work: whether the judgements the sixteenth pass wrote into the property gate are **true**, at
`ceeddc3`, `Merge pull request #150 from Niizuki/channel-0.2-condition-4-sixteenth-pass`

Date: 2026-09-10

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **seventeenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not clean.** Its frozen set reported nothing, for the eleventh consecutive pass. The instrument
it built found **BC1** and **BC2** in the package on its first run, so the ruling's second test is not
met and the two-consecutive count stays at zero. It raises **BC3** against its own new code; under the
ruling that belongs to neither counted population, and it is numbered because the correction cites it.

The method was not chosen by this pass. The sixteenth named it and named it against its own work: a
`Read-Optional` says an absent field is a fact the design states, the census checks that the
declaration was **made** and the check beside it that some input **exercises** it, and neither can
check that it is **right**. Five declarations stood. A declaration that is exercised and wrong looks
exactly like one that is exercised and right.

**Two of the five were wrong**, and what made them wrong is visible without reading a single artifact:
the only inputs that leave those fields absent are the very vectors the reading property is declared
**red** on.

## Section numbering

**BC1** and **BC2** are what the instrument this pass built reported about the package on its first
run. **BC3** is a defect in this pass's own new code, found by a frozen instrument in the way BB6 and
AZ3's sweep found theirs.

The tally in the plan counts all three, because that is what a reader counts. Where the ruling's two
populations differ from the total, the split is stated rather than the total adjusted.

## The frozen set, run first

**Zero.** The design gate, the owned-fact gate, the return-channel census, the coverage measure, the
whole 103-probe corpus, the declared property corpus, the 2,000-vector generated run, AZ3's
dropped-field sweep and the read-provenance census were all green before any of this pass's work
existed. Eleventh consecutive clean frozen set, and strictly larger than the sixteenth's: it gained
the read-provenance census and the six probes that pass added.

It is reported with the caveat every pass since the fourteenth has had to make, and this one is the
plainest yet. **The frozen set could not have reported the class below, and the sixteenth pass said so
in advance.** Every instrument here asks whether a check runs, whether a guard fires on its own
subject, whether a property is red where it should be green, or which reader performed a read. None of
them reads what a reader's stated *reason* claims. A declaration that says the opposite of the contract
it serves is declared, exercised, correctly routed, and green.

### BC1 — two declarations are exercised only by the mutation they were written beside

`C6-P1` reads the three parts of an authority presentation. Its second clause is about a presentation
that **omits** one of them, so the two parts that can be omitted were read through `Read-Optional`:

```
-not (Read-Optional $record 'decisionPoint' $presentationIsAbsence) -or
-not (Read-Optional $record 'initiatorAttribution' $presentationIsAbsence) -or
```

and the reason both cited was *the clause is about an authority presentation that OMITS one of its
three parts, so an absent field is the violation being detected and not a vector that failed to state
one*.

Read that against the reader's own contract, which the sixteenth pass wrote four lines above it:
`Read-Optional` means **absence is a fact the design states**. The reason says absence is *the
violation being detected*. Those are not the same claim; they are opposite claims, and the reason
states the second one in its own words while the reader declares the first.

The contract settles it in one sentence. `C6-P1` is, verbatim:

> No C6 vector reaches handler dispatch unless one exact local authority decision is `permitted`;
> every denial or unevaluatable presentation **records** the decision point, initiator attribution,
> and `known-none`.

There is no conforming record in which either field is absent, because the property's own statement is
that every denial records them. The declaration was not a judgement that turned out wrong on a
technicality — it asserted the negation of the sentence it was written to enforce.

**Measured rather than argued.** The instrument records, for every absence either meaning-carrying
reader observes, the verdict the input producing it is **declared** to yield. Against the package as
this pass found it:

| field | inputs that leave it absent | declared verdict |
| --- | --- | --- |
| `refusal` | 43 interaction records across the corpus | green |
| `provenanceFormActually` | 48 of 49 interaction records | green |
| `terminalHistoryChangedBy` | 48 of 49 interaction records | green |
| `decisionPoint` | `C6-denial-without-decision-point`, and nothing else | **red** |
| `initiatorAttribution` | `C6-denial-without-initiator-attribution`, and nothing else | **red** |

Three of the five are exercised by conforming records, many of them, which is what a fact the design
states looks like. The other two are exercised by exactly one input each — **the property's own named
mutation for the clause that reads them.**

That is what BB5's check could not ask. BB5 killed nine declarations no input exercised at all; it
counts an absence wherever one occurs, and an absence occurs on a declared mutation as readily as on a
conforming input. A declaration satisfied only by the vector written to make the property go red is a
declaration about a violation, and BB5 passed both.

**The correction is a fifth reader, not two edits.** The four readers differed in what an absent field
means — empty, unevaluable, a stated fact, or a widened candidate set — and none of them meant *the
violation this clause detects*, which is a real and separate meaning that `C6-P1` needs. `Read-Obligation`
is that meaning written down, and its falsification requirement is the **mirror** of `Read-Optional`'s:

- a `Read-Optional` needs an input the property is declared **green** on to leave the field absent, or
  the absence it calls a fact is one no conforming record produces;
- a `Read-Obligation` needs an input the property is declared **red** on to leave it absent, or nothing
  in the suite demonstrates the clause catches the omission — which is AR1's unfalsifiable clause
  arriving through the reader instead of through the mutation table.

Both are checked. Two declarations moved, three stayed, and the reason text moved with them: what the
two now cite is `C6-P1`'s own sentence rather than a claim about vectors.

**What this does not fix, and it is stated in the file rather than discovered later.** A vector that
omits the field because it models a realization that omitted it, and a vector that omits it because its
author did not write it down, are still the same bytes. This reader **names** that; it does not close
it. Closing it needs the vector to state the omission positively, which changes what a conforming
authority record must carry — an owner question, recorded below rather than decided here.

### BC2 — `C6-P1`'s second clause had no input it is declared green on

Correcting BC1 meant asking which inputs reach the clause at all. Four do, and the answer is the
finding: **every one of them is one of the clause's own mutations.**

| input | reaches clause 2 because | declared |
| --- | --- | --- |
| `C6-delivery-treated-as-permission` | decision `unevaluatable` | red, through clause 1 |
| `C6-denial-without-decision-point` | decision `denied` | red |
| `C6-denial-without-initiator-attribution` | decision `denied` | red |
| `C6-denial-with-possible-effect` | decision `denied` | red |

The three green members — `S-conforming-single-session`, `S-two-sessions-conforming` and
`S-conforming-fault-from-established` — all record `permitted`, so the evaluator returns before the
clause. **No declared input in the corpus is a denial that satisfies what `C6` requires of one.**

The clause was covered by the coverage measure, falsifiable through all three of its operands after
AT2, and had never once been observed staying green. That is AE3's rule — *nothing required a property
to stay green, only to be able to fail* — one level below where AE3 put it. AE3 bound the **property**;
this is the same gap at the **clause**, where a property with a required-green set can still hold a
clause every green member skips.

Corrected by adding `C6-conforming-denial-records-all-three`: the same realization as the mutation it
sits beside, with the decision point recorded. It is an additional-green member of `C6-P1`, and it was
pinned before it was believed — removing the decision point again takes it red **through
`C6-P1-clause-2`**, with the witness naming the clause, which is what shows the vector reaches the
clause rather than passing it the way the three session vectors do.

### BC3 — the first draft hid a declared channel from the census that checks it

Both meaning-carrying readers accumulate the same way, so the first draft factored the six lines into
one helper and passed the accumulator in as a parameter.

The return-channel census — a frozen instrument — failed:

```
'verify-channel-0.2-properties.ps1' declares the accumulator '$script:OptionalReads' and nothing
in the gate adds to it. The declaration has stopped applying and must be re-anchored or deleted
with the code it was written for.
```

It was right, and it was right for the reason **BB3 wrote down one pass earlier**. The census
recognises a producer by a write to the `$script:` name; BB3 widened it from `.Add(...)` to index
assignment as well, and stated the limit that remained — *a write through an alias is still invisible*.
A collection reached through a parameter is that alias, and the draft walked into the limit in the
first commit after the sentence describing it.

Corrected by writing the accumulation out at each reader. The duplication is six lines; what it buys is
that a declared channel stays visible to the instrument that checks the channel, which is worth more.
The polarity value itself is still computed once, in a function that returns a value rather than
receiving a collection.

## The instrument, and what it reports

Two new units in `build/verify-channel-0.2-properties.ps1`, beside BB5's exercise check, because they
need the evaluators and the evaluators live there.

**How it measures.** The declared-corpus dispatch is the only one of this file's five whose inputs
carry a stated expectation, so before each evaluation it publishes that input's declared verdict, and
each meaning-carrying reader records that verdict whenever the field it reads is absent. A declaration
therefore accumulates the set of polarities its absences came from, rather than a flag saying one
occurred. The check reads the set.

**Why the declared verdict and not the observed one.** The observed verdict is the property's answer;
the declared verdict is what the corpus says the answer must be. Classifying by the observed one would
let a property that is wrongly green certify the declaration that made it wrong — the same circularity
as measuring a guard by whether today's corpus makes it fire, which is AP1's class and why the coverage
measure counts conditions rather than failures.

It reports **3 `Read-Optional` and 2 `Read-Obligation` declarations, each checked against the declared
verdict of the inputs whose silence exercises it**, where the package as found reported five of the
former and none of the latter.

That sentence is the second draft, and the first was caught by re-reading the diff rather than by
anything in the gate. It said *3 `Read-Optional` declarations exercised by a conforming input*, which
is the verdict of the check and not a property of the count — and the line prints whether or not the
checks above added a failure, so on the run that fails one of them it would have asserted precisely
what that run had just contradicted. The measure now says what it measured.

**Two limits, stated in the file where they apply rather than discovered later:**

- it reads the **polarity** of the exercising input, not the artifact. A declaration whose reason cites
  a contract sentence that does not say what it claims is exercised, correctly polarised, and still
  wrong. Nothing here closes that, and the reason is written at the call site, which is where a reader
  audits it; and
- an absence observed outside the declared-corpus dispatch — in the generated loop, either dispatch of
  the operand harness, or AZ3's sweep — is recorded as `undeclared` rather than guessed at. Those
  populations have no stated expectation to read.

## Findings

| id | where | what |
| --- | --- | --- |
| **BC1** | package | two `Read-Optional` declarations are exercised only by the named mutation of the clause that reads them, and each asserts the negation of the contract sentence that clause enforces |
| **BC2** | package | `C6-P1`'s second clause had no input it is declared green on: all four inputs that reach it are its own mutations |
| **BC3** | this pass's own | the first draft passed a declared accumulator through a helper parameter, hiding it from the return-channel census — BB3's own stated limit, one pass later |

## What this pass verified rather than believed

- **The new check was made to fail for its claimed reason before the correction was believed.**
  Reverting the two call sites to `Read-Optional` on the corrected gate fires it on exactly those two
  declarations, naming the field, the reason, and that every input leaving it absent is one the
  property is declared red on.
- **Both new units are pinned by probes**, taking the corpus from 103 to 105. `BC1-a` puts a
  `Read-Optional` back where the absence is a violation; `BC1-b` points a `Read-Obligation` at
  `refusal`, whose absences are all on conforming inputs, and the mirror check fires. Each was run and
  returned the verdict its guard owes.
- **And `BC1-b`'s key then went stale inside this same pass, which is AP1's class committed by the
  author of the probe.** The obligation check's failure message was rewritten two corrections later —
  a nested conditional in it was reachable only on a failing run, so the coverage measure reported it
  as never executed and it was removed rather than exempted — and the probe still keyed on a phrase
  that rewrite deleted. **A probe whose key was correct when written stops being correct when the work
  moves**, and four passes have now produced an instance. It is re-anchored on the guard's stable
  claim rather than on its wording, and the corpus is what reports the next one.
- **BC2's vector was pinned to reach the clause, not merely to be green.** Removing its decision point
  takes `C6-P1` red through `C6-P1-clause-2` with the clause named in the witness; the three session
  vectors never reach that clause at all.
- **BC3 was found by a frozen instrument and not by inspection**, which is the whole argument for
  keeping the return-channel census in the per-commit gate.
- **The recomputed measures caught the corpus change before a reader did.** Adding one vector took the
  gate to 132 evaluations over 56 declared inputs, and the plan's section 4 measures failed on the two
  figures stated in prose until they were recomputed. That is AM2's machinery working.
- **The design gate's five entry-point findings were the gate working, not the pass failing.** The
  narrative tally, the next-work sentence, the provenance table's missing `BC` row, the Channel index
  row, and the retained-review list each failed on the commit that made them stale, which is the AJ2
  class caught by machinery rather than by the ninth consecutive cycle of reading.

**One correction was made on contact rather than found by the instrument, and it is recorded here
because it is a claim in a guard.** The retained-review check's own failure message read *all sixteen
retained attestations, and all eighteen iteration reviews*. There are twenty retained iteration
reviews, and there have been since the AV pass: the message had been wrong by two for three families
while the check itself was correct, because the expected list beside it is what the check compares and
the tally was decoration. The message now states the rule and points at that list. This is AN1's class
— a guard whose comment claims more than its code — inverted: here the code was right and the sentence
describing it was not.

## What remains outside the pass

**The declaration's reason is still unchecked against the artifact.** This pass checked the
**polarity** — that the inputs exercising a declaration have the verdict the declaration's kind
implies — and polarity is a proxy. `provenanceFormActually` and `terminalHistoryChangedBy` are named by
**no design artifact at all**: the rules *an absent actual form means agreement* and *an absent changer
means no sibling changed it* are the gate's own conventions, correctly polarised and cited nowhere. The
next unit is the citation: a declaration names the artifact and the words that settle it, and the gate
checks the words are still there. That is the sixteenth pass's question with the easy half removed.

**The generated population still carries one frame shape per session**, and the generator still
produces conforming vectors only, with the mutation direction applied by hand and discarded. The
fourteenth left this, the fifteenth and sixteenth did not reach it, and neither did this one.

**The census still does not reach the other four dispatches.** BC1's polarity is recorded only where a
declared expectation exists, which is the same boundary the read-provenance census draws and for the
same reason.

**Cost.** Both units run inside a dispatch that already happens, and the properties gate at
`-GeneratedCount 0` is unchanged within measurement noise. The one measurable cost is the corpus: one
more declared input is one more evaluation for `C6-P1` and 55 more poisoned sites for the census.

## The open question this pass puts to the owner rather than deciding

**Should a vector state a realization's omission positively?** `Read-Obligation` names the ambiguity it
inherits and does not close it: a vector that omits `decisionPoint` because the realization omitted it
is byte-identical to one whose author did not write it down, and AU2's rule is that an obligation
cannot tell those apart. Closing it means the authority record carries what the realization recorded
rather than the reader inferring it from what is missing — which changes what a conforming record must
state, and therefore what the neutral brief's vector format says. That is a choice between defensible
designs, which S1 and R1 established is an owner ruling and not an author's call.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, seven, seven, **three** — two of
this pass's three in the package and one in its own new code.

## Where this family is dispositioned

**BC1** through **BC3** are corrections to the verification instruments, their declarations, one
declared input and one property's green set — not to the design — so under the 2026-08-20 ruling they
belong in the verification foundation plan's own record and not in the completeness review's
disposition index. The plan's section 2t carries them, and this document is the pass's evidence.
