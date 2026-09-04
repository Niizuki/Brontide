# Channel 0.2 thirteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-thirteenth-pass-2026-09-04-182c95c`

Reviewed work: `C4-P2` as it is evaluated over generated input, at `182c95c`,
`Merge pull request #146 from Niizuki/channel-0.2-condition-4-twelfth-pass`

Date: 2026-09-04

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **thirteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names.

This pass records no finding against the package.

## The frozen set, run first

**Zero.** 80 probes, the coverage measure and the 2,000-vector generated run, all green before any of
this pass's work existed. Seventh consecutive clean frozen set, and the second in a row that is
**strictly larger** than its predecessor's — it gained the three probes the twelfth pass retained.

## The method, and what changed

`C4-P2` is the property this programme has been about: S1, U1, V1, V2, W1-W6, X1, X5, Y1-Y4, Z1-Z3,
AC1, AE1, AF1, AG1, AI1, AJ1, AK1, AK5, AK6 and AL2 all converged on it. On generated input it was
**vacuously green**: both conjuncts iterate observation records, the generator produced none, and a
loop over an empty collection returns green having asserted nothing.

The generator now emits C4's frame-level view — declared stimulus steps with commit indices, their
delivery dispositions and arrival ordinals, the recipient's admitted-identity sets, an `unseen`
refusal record and a settled late-traffic latch — and the vector carries the step index those
references resolve against.

Both records are built to be **conforming while still being examined**, which is the whole difficulty:

- The refusal is of a cancellation control naming an identity **no request ever opened**, which is the
  legitimate `unseen` case the design keeps `rejected-protocol` for. The conjunct's four selectors
  match, the reference resolves, the request set it then searches is empty, and it is green on a
  record it actually read.
- The latch settles against a frame committed **after** the endpoint's own terminal frame, which is
  what late traffic is. The comparison runs over both resolved operands and finds nothing.

**Both conjuncts now fire on generated input**, which is the result this pass exists to produce:

| Mutation of the generated observations | Verdict |
| --- | --- |
| the control names an identity the request opened and the recipient admits | red through `C4-P2-conjunct-1`, witnessing the request committed before the refused control |
| the latch settles against a frame committed before the terminal | red through `C4-P2-conjunct-2`, witnessing the settling frame committed first |

Neither mutation was reachable before this pass at any vector count.

**The population keeps its records, asserted rather than assumed.** Two further required shapes are
declared — an `unseen` refusal carrying the four values the first conjunct selects on, and a settled
latch carrying both frame references — and each was checked by breaking it: a refusal whose detailed
reason the conjunct does not select, and a latch that is never settled, each make the assertion fire.
A record the conjunct skips proves as little as no record at all, which is why the shapes are keyed on
the selector values rather than on a record merely existing.

**Result: 0 red over 2,000 generated conforming vectors, 52,000 evaluations**, with `C4-P2` among them
non-vacuously for the first time.

## Findings

None. The frozen set reported nothing, and the instrument this pass extended found nothing in the
package.

Two defects were found in the **instrument** before it was believed, and are recorded as what the
falsification is for rather than as findings against the design. The first mutation written for
conjunct 2 did not reorder the frames as intended and reported no red — it was the mutation that was
wrong, not the conjunct, and a pass that had stopped at "no red" would have recorded a false negative
about the property it was there to reach. The second is that a reference which resolves to no declared
step produces an *error* rather than a red, so a generator emitting records whose references do not
resolve would have reported failures rather than a silent pass; that is `C4-P2`'s own AK6 machinery
working on generated input.

## What this pass verified rather than believed

- **The frozen set was run first**, and is strictly larger than the twelfth pass's.
- **Both conjuncts reach a verdict on generated input.** Each was made to go red through its own
  named conjunct, which is the only evidence that distinguishes a conjunct that is satisfied from one
  that is skipped.
- **The records survive**: two shape assertions, each checked by breaking the record it names.
- **The references resolve.** An unresolvable one raises an error the run would report, and the run is
  clean.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs.

## What remains outside the pass

**The generated `C4-P2` inputs are one shape each.** Every generated session carries the same refusal
and the same latch, varying only in session id and ordinals. That reaches both conjuncts, and it does
not explore them: the operand corrections AF8, AG2, AH1, AI1, AJ1, AK1, AK5 and AK6 are each about a
reference that has **lost a field**, and a generator that always publishes all five fields tests none
of those. Generating references with fields dropped is the obvious next increment, and it is what
would turn the nine retained operand mutations into a rate.

**The generator produces conforming vectors only.** The mutation direction used here and in the two
passes before it is still applied by hand and discarded.

**`C4-P1`'s in-flight bound remains scoped to one named profile**, and the direction scope for a
profile in which both endpoints initiate is still undecided — a limit the completeness review states
rather than one this pass changed.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, **zero**.

## Where this family is dispositioned

No family is raised, so there is nothing to disposition. The pass's record is this document and the
plan's section 2p.
