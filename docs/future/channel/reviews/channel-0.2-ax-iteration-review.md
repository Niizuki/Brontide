# Channel 0.2 twelfth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twelfth-pass-2026-09-04-65a2b80`

Reviewed work: the Channel 0.2 design's own claims through the twenty-six executable properties, and
the entry points that route a pass to its work, at `65a2b80`,
`Merge pull request #145 from Niizuki/channel-0.2-condition-4-eleventh-pass`; raised and corrected
AX1-AX2

Date: 2026-09-04

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twelfth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it** under either reading, because the instrument it extended found
a real defect in the package — which is the ruling working rather than failing.

## The frozen set, run first

**Zero.** 77 of 77 probes, the coverage measure, and the 2,000-vector generated run, all green before
any of this pass's work existed. Sixth consecutive pass with a clean frozen set — and the first whose
frozen set is **strictly larger** than its predecessor's, because the generator built by the eleventh
pass joined it under the quarantine rule.

## The method: teaching the generator to refuse

The eleventh pass left the generator producing no refusal of any kind, so three property clauses were
evaluated by nothing at all: `I4`'s first clause and `C5-P1`'s second both gate on a refusal, and
`C6-P1`'s second gates on an authority decision that is not `permitted`. With every generated
interaction dispatched and permitted, none of the three had an input.

One admitted interaction in four is now refused before dispatch. It is a legal realization: the
refusal records `known-none`, which is what `I4`'s first clause and `C5-P1`'s second each require of
it; the denial carries its decision point, initiator attribution and `known-none`, which is what
`C6-P1`'s second clause requires; the provenance form is `local-pre-dispatch-refusal`, one of the four
`C9-P1` closes over; and there is no post-dispatch path, which is why `C10-P1` does not ask for
evidence narrowing one.

**Result: 0 red, and the new inputs demonstrably reach the clauses they were added for.** Three
mutations confirm it, each caught by the property that owns the rule:

| Mutation of the refusal | Properties red |
| --- | --- |
| the refusal records a certainty other than `known-none` | `I4`, `C5-P1` |
| the denial omits its decision point | `C6-P1` |
| the refusal selects a provenance form outside the four | `C9-P1` |

The generator now asserts a sixth required shape — an interaction refused before dispatch — and fails
without it, which was checked by switching refusals off and watching the assertion fire.

## Findings

### AX1 -- the entry points went stale in the commit that recorded the eleventh pass -- corrected

Four sentences across two documents still described the eleventh pass as the work ahead, after it had
run and been retained beside them: the plan's condition-4 tally said ten, the review policy's pass
count said ten, and both named the **eleventh** pass as the next one. A fifth surface, the review
policy's compact list of retained passes, stopped at the tenth.

**The split is the one this programme has now recorded nine times.** The Channel index's counts were
correct, because a gate recomputes them; the plan's tally and the review policy's count are prose, and
nothing read them. This is AA1's correction, which made the index row structural, never applied to the
two surfaces that tell the next agent which pass it is running.

**Corrected, and the class is closed rather than the five instances.** The design verifier now counts
the retained condition-4 passes — keyed on what a review calls itself in its own title, not on its
filename, because a filename key is a lexical key over a naming convention and that is the shape AL1
and AT1 were each raised against — and requires both the plan's tally and the review policy's count to
state that number, and both next-work sentences to name the ordinal one past it. It failed with
exactly the four sentences found by reading, before they were corrected.

### AX2 -- an interaction record states a dispatch nothing reconciles against the timeline -- corrected

Mutating the generator to mark a refused interaction `dispatched = true` — a vector disagreeing with
itself — left **every property green**. The reason is that `dispatched` on an interaction record is
read by nothing: all six properties that care derive dispatch from the timeline's `dispatch` steps.
Forty-nine declared interaction records carry the field.

So a vector could record an interaction as dispatched while its timeline never dispatches it, read to
a human as one thing and evaluate as another, with nothing to notice. That is the **W1 class** — one
fact, two surfaces, maintained by hand — on a field small enough that no pass had looked at it, and it
is the class W1 spent a whole work item retiring for the frame references.

**Corrected** by reconciling the record against the timeline rather than by deleting the field, which
is what a reader of the record sees. No declared vector disagrees today, so this was latent; the check
was pinned by flipping the field on the conforming single-session realization and watching it fire.

## What this pass verified rather than believed

- **The frozen set was run first**, and it is strictly larger than the eleventh pass's.
- **The new inputs reach their clauses.** Three mutations, each caught by the owning property — an
  extension that added shape without reach would have shown as no property going red.
- **The refusal shape assertion is load-bearing**: switching refusals off makes it fire.
- **AX1's check fails on the stale text** it was written for, and **AX2's** on a vector made to
  disagree with itself.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs.

## What remains outside the pass

**`C4-P2` is still evaluated over empty observation records.** The generator produces no recorded
`unseen` refusal, no late-traffic latch and no declared stimulus steps, so both of its conjuncts remain
**vacuously green** on generated input. Pre-dispatch refusals are a different record from the `unseen`
refusal that conjunct quantifies over, and reaching it needs declared steps with commit indices and
five-field frame references. That is the next increment, and it is the one that matters most: `C4-P2`
is the property eight finding families have been about.

The mutation direction that falsifies the generator is still not retained. The tenth pass's residue —
the 21% guard coverage, `guardMessage` asserting presence rather than exclusivity, and the
`Stop`-preference hazard outside the design gate's git calls — is still not discharged.

**AW2 has two more instances and remains open.** AX1 came from reading, not from the frozen set and
not from the new instrument, so it belongs to neither population the 2026-09-04 ruling counts. AX2 did
come from the new instrument, so the ruling classified that one cleanly. The gap is now the difference
between a pass that would have met condition 4 and one that plainly does not, and it is still an owner
decision.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, **two**.

## Where this family is dispositioned

AX is a `verification` family. AX1 is in the entry points and the design verifier, AX2 in the vector
format's reconciliation and the properties gate; neither reaches a design artifact, so the disposition
belongs in the verification foundation plan.
