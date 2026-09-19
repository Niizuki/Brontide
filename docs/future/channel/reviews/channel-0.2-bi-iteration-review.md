# Channel 0.2 twenty-third W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twenty-third-pass-2026-09-20-ed2145a`

Reviewed work: the operand census sharpened from the field to the property -- every field a property
reads on its own declared inputs, against whether its value decides that property's verdict on any of
them -- and the mutations, evaluator correction and witness that answer what it found, at `ed2145a`,
`Merge pull request #157 from Niizuki/claude/next-item-implementation-c74c67`; raised and
dispositioned the BI1-BI3 findings this document records

Date: 2026-09-20

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twenty-third** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not.** The frozen set reported nothing at `ed2145a` -- the third consecutive clean frozen set
-- and the instrument this pass built found **two things in the package** on its first run, one in
the design's own evidence and one in the verification. Under the ruling's two populations that is
the outcome that resets the count, and the count stays at zero. **BI3** was found by reading and is
pinned; its pin found nothing beyond it.

## Section numbering

**BI1** and **BI2** are what the instrument reported on its first run over the package as found,
before anything was corrected: three (property, field) pairs, two of them one property's. **BI3** is
an entry point found by reading while recording the pass, in the population the 2026-09-16 ruling on
AW2 governs: it is corrected, its class is pinned in the design gate, and the pin's first run found
only the instance it was written for, so under that ruling it does not fail the pass -- which BI1
already does.

## The frozen set, run first

**One.** In a short-path clone at `ed2145a`, before any of this pass's work existed: the text and link
guards, the design gate, the owned-fact gate, the return-channel census, the properties gate at its
default count and at the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors at
0 red, AZ3's dropped-field sweep over 500 of them with all eight load-bearing droppings reporting
where declared, the read-provenance census over both polarities at 0 raw, the field-readership census
at 0 read by nothing, the operand census at 84 fields tried and 0 inert and undeclared, the
closed-vocabulary census over both populations, the declaration-polarity and declaration-citation
checks -- the coverage measure over four gates with the 46 condition and 6 operand exemptions it
declared before this pass, and the 141-probe corpus at FROZEN-CORPUS over 22 workers. All exit 0.
The streak of clean frozen sets stands at three.

## The instrument

The operand census the twenty-second pass built asks whether a field's value decides **some**
property's verdict on **some** input, and its record left the sharper question named and not asked:
a field one property decides on and another only reads is decisive in the corpus and inert for the
second property, and the field-level verdict cannot tell. This pass asks it. The census now keeps its
tallies per (property, field) as well as per field, and every field a property reads on its own
declared inputs must decide that property's verdict, conjunct, errors or unpublished fields on at
least one of them -- or be named in the property's witness, which the trials show as a red input
whose witness text moved and nothing else did. A field inert for every property is the field-level
verdict's subject and is not reported twice.

Why this is AU1 one operand down. AU1's check requires every obligation a property enforces to fire
on one of its declared inputs. An obligation fires when its expression is true, and an expression
with two operands can be true on every red input because of one of them: the other is read, compared
on every input, and never the reason. An evaluator that ignored it would pass every input the
property declares, and nothing in the suite would distinguish that evaluator from the one written.

**On its first run over the package as found it reported three pairs**, over 166 (property, field)
pairs of which 137 were decisive for their property and 17 named in its witness. Two are `I5`'s. One
is `I6`'s. The instrument was then refined once in the course of correcting BI1, as recorded there.

### BI1 -- I5's terminal handling was exercised by no input of I5, and the evaluator was wrong underneath it

`I5` reads, on each terminal step of a session's timeline, whether the terminal fact was accepted and
which identities it closes, and frees the live slots accordingly. On every input `I5` declared,
neither ever decided the verdict: 12 trials on `accepted` and 4 on `closes`, all on green inputs,
none decisive. The inputs say why. Each required-green member holds one interaction against a bound of
two, so no terminal's handling can matter; the one named mutation, `I5-concurrency-exceeds-bound`,
admits two interactions before any terminal arrives, so it is red before the handling runs. An
evaluator that freed a slot on any terminal fact, refused or not, whatever it said it closed, would
have passed every input the property declares. No terminal step anywhere in the corpus was
unaccepted.

Writing the two mutations the property was owed found the evaluator wrong as well. It kept a count per
session: an admission added one and an accepted terminal subtracted however many identities it named.
So `I5-terminal-closes-other-identity` -- the accepted terminal for `i1` closes `i2`, an identity the
session never admitted, and the session then admits `i2` against a bound of one -- was **green**: the
terminal freed `i1`'s slot in the count, and the property passed a session holding two against a
bound of one. That was seen before the evaluator was touched, as the declared loop reporting the
property green where the vector declares red. The evaluator now tracks the set of admitted identities
no accepted terminal has yet closed, `I5-refused-terminal-holds-slot` fires through `accepted`,
`I5-terminal-closes-other-identity` fires through `closes`, and the completeness review's audit row
for `I5` names both, which is the design-artifact edit that classifies this family `design`.

**The correction reached the instrument too.** With the mutations in place, `closes` was still inert
for `I5` by the census's measure, because the census drew an identifier's wrong values from the other
values of its own path in the vector, and the value that repairs that mutation's red -- the admitted
identity `i1` -- is stated at `sessionTimeline[].identity` and never at `closes`. The design names
the spaces those identifiers belong to, so the census now draws an identifier's siblings from its
identity space: session identities, interaction identities and stimulus-step identities, each space
declared with the brief's words, checked to list identifier fields only and no field twice. Trials
went from 3,540 to 3,652 and no field changed class at the field level; `closes` became decisive for
`I5` through the mutation written for it.

### BI2 -- I6's witness named the interaction identity alone

`I6` read the interaction's session into the subject of its `Read-Required` diagnostics and into
nothing else, so on 8 trials the value moved neither verdict nor witness. The witness named the
identity alone -- "relational interaction i1 matches 2 declarations" -- and one interaction identity
may be open in two sessions at once, which is AK1's lesson. Both witnesses name the session now, and
the one probe that anchored on the old text moves with it. No design artifact is touched.

### BI3 -- the future index's Channel row named the next pass the twenty-first, for three passes

The future-work index's Channel row ends "the closure cycle remains on hold while a twenty-first
verification-foundation pass is next". It was written when the twentieth pass landed and was right
then; the twenty-first and twenty-second passes moved every other surface that names the next pass
and left it, because the design gate reads that row for the newest design family's token and for
nothing else, and both of those families were `verification`. That is AX1's class a fifth time, and
BG2's exactly: the surface that goes stale is the one no guard reads.

Corrected, and the class pinned: the design gate's next-pass ordinal check, which reads the plan's
and the review policy's sentences, now reads the row's as well. Restored to "twenty-first" the pin
fires -- "calls the next pass the twenty-first and 22 have been retained, so the next one is the
twenty-third" -- and over the package it reported nothing else, so the instruments had converged
there and this finding does not count against the pass under the 2026-09-16 ruling.

## Findings

| id | where | what |
| --- | --- | --- |
| **BI1** | the corpus, the evaluator and the completeness review's audit row, by the instrument's first run | `I5` read `accepted` and `closes` on terminal steps and neither decided its verdict on any input it declared; the evaluator counted terminal facts and was green on a terminal closing an unadmitted identity; two mutations, a set-tracking evaluator, and identifier siblings drawn from the identity space |
| **BI2** | the gate, by the instrument's first run | `I6`'s witness named the identity and not the session, so the session read decided nothing and named nothing; both witnesses name it |
| **BI3** | an entry point, by reading | the future index's Channel row naming the next pass the twenty-first for three passes; corrected, and the row joins the design gate's next-pass ordinal check, whose first run found only this instance |

## What this pass verified rather than believed

- **The frozen set was run at `ed2145a` before any of this work existed**, in a short-path clone, and
  everything reported above was read from that run's output. The deep properties run took 873
  seconds; the coverage measure and the corpus figures are in the frozen-set paragraph above.
- **The instrument was made to fail for each of its claimed reasons before its verdict was believed**,
  and six probes keep the corpus honest about it: `BI-a`, `I5` freeing a slot on any terminal fact,
  reported as reading `accepted` and never deciding on it beside the declared loop's own failure;
  `BI-b`, the count-based defect restored, reported the same way on `closes`; `BI-c`, both of `I6`'s
  witnesses returned to naming the identity alone, reported on `session`; and `BI-d`, `BI-e` and
  `BI-f`, an identity space listing a non-identifier, citing words the brief lacks, and listing one
  field twice, each refused. Each was run alone and seen to return the verdict it owes. The corpus
  goes from 141 to 147.
- **BI1's evaluator defect was seen before it was fixed**: with the two vectors declared and the
  count-based evaluator still in place, the gate reported `I5` green on
  `I5-terminal-closes-other-identity` where red is declared, and red already on
  `I5-refused-terminal-holds-slot`.
- **BI3's pin was seen red on the stale ordinal** in a copy of the index before the row was
  corrected, and reported nothing else over the package.
- **What the generated population would reach was measured rather than argued.** The coverage measure,
  run over the properties gate with the generated count set to zero, reports as never evaluated only
  constructs inside the generated block itself: no conditional of any evaluator is reached by a
  generated vector and by no declared one. The limit of that measurement is stated below.

## What remains outside the pass

**The witness criterion is a proxy.** A field a property both names in its witness and compares in a
test that never fails reads as witness-named. Separating the two uses needs the trials to see which
statement read the value, which is the read-provenance census's method and not this one's.

**Both censuses still walk the declared corpus.** The measurement above says the generated population
reaches no evaluator *conditional* the declared corpus does not; a branch body with no conditional of
its own is below what that measure sees, and a wrong value on such a body is the case the generated
walk would still be for. The sixteenth pass's trade stands unmade, now with its expected yield bounded.

**The generator emits accepted terminals only**, so the operand BI1 made decisive on the declared
corpus is exercised over the generated population by nothing; a refused terminal in the generated
wave is a shape the generator does not yet produce.

**BH3 is still the owner's**, as open question 5 of the plan.

**The three narratives' finding ranges** are still read by no guard for the range.

**Cost.** The per-property tallies add nothing measurable to the properties gate; the identity-space
siblings add 112 trials. The corpus gains six probes at `-GeneratedCount 0`.

**Whether this pass is the first of the two owed is not in question.** The instrument found BI1 and
BI2 in the package on its first run, which is the ruling's second population, and the count stays at
zero; BI3 is by reading and its pin found nothing beyond it. The closure review remains on hold. The
finding count by condition-4 pass is now five, six, three, two, five, one, seven, seven, five, three,
one, three, zero, three, seven, seven, three, three, five, twelve, two, five and **three**.

## Where this family is dispositioned

BI1 reaches the completeness review's per-capability audit row for `I5`, so the family is classified
`design` and is dispositioned in that review's
[disposition history](../Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md#review-disposition),
with the verification foundation plan's section 2z carrying the pass; this document is its evidence.
