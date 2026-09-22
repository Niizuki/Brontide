# Channel 0.2 twenty-fourth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twenty-fourth-pass-2026-09-22-30e3f10`

Reviewed work: the generated population given terminal facts that are refused before the one that
closes the interaction, so that the operand BI1 made decisive on the declared corpus is exercised
over the population as well, at `30e3f10`, `test: run the composition roots' suites four tests at a
time`, which is `a4497b5`, `Merge pull request #158 from Niizuki/claude/condition-4-twenty-third-pass`,
plus the seven commits of the repository-gate optimisation branch, none of which touches a design
artifact, a conformance declaration or a Channel 0.2 gate other than by making the properties gate's
two tree walks iterative

Date: 2026-09-22

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twenty-fourth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

This pass records no finding against the package.

**It is the first of the two.** The frozen set reported nothing at `30e3f10` -- the fourth
consecutive clean frozen set, and a strictly larger one than the twenty-third's, having gained that
pass's six probes -- and the instrument this pass built found nothing in the package on its first
run. Under the ruling's two populations that is the outcome the count is waiting for, and the count
is one. The ruling asks for two, the second over a strictly larger frozen set, and the twenty-fifth
pass is where that is decided.

## The frozen set, run first

**Zero.** In a short-path clone at `30e3f10`, before any of this pass's work existed: the design
gate, the owned-fact gate, the return-channel census, the properties gate at its default count and at
the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors at 0 red, AZ3's
dropped-field sweep over 500 of them with all eight load-bearing droppings reporting where declared,
the read-provenance census over both polarities at 0 raw, the field-readership census at 0 read by
nothing, the operand census at 84 fields tried and 0 inert and undeclared, the closed-vocabulary census
over both populations, the declaration-polarity and declaration-citation checks -- the coverage
measure over four gates with the 47 condition and 6 operand exemptions it declared before this pass,
and the 147-probe corpus at 147 of 147 over 22 workers. All exit 0. The head is the twenty-third pass's merge plus the seven commits that took the
repository gate from 735 to 235 seconds, whose only edit to a Channel 0.2 gate replaced the properties
gate's two recursive tree walks with a stack; the twenty-third pass's frozen set was green at the
merge, and this one is green above it.

## The instrument

The twenty-third pass left it named: the generator emits accepted terminals only, so the operand
**BI1** made decisive on the declared corpus -- whether a terminal fact was accepted -- is exercised
over the generated population by nothing. Every evaluator that reads a terminal step met the refused
value on red-expected declared inputs alone, which is one side of the operand: the declared mutations
say what a property must refuse, and no input said what it must accept.

The generator now refuses one in four terminal facts before the one that closes the interaction. That
is the capability contract's failure clause for terminal facts, "missing, extra, wrong-session, or
mismatched identities reject the claimed terminal fact," applied to a dispatched interaction: the
rejected claim leaves the interaction nonterminal, awaiting a valid one, so the identity keeps its
slot -- it is still in the wave and still counted live -- and reaches its one terminal history when
the wave closes, exactly as it would have. The population is required to carry the shape, keyed on
the refusal itself rather than on a terminal step existing, since every vector already carries the
latter.

**On its first run over the package as found it reported nothing**: 0 red over 100 generated
conforming vectors carrying the shape, 26 of 26 properties, with the required-shape check satisfied;
and 0 red over the deep run, 52,000 evaluations over 2,000 generated conforming vectors, AZ3's sweep
over 500 of them evaluated 8,541 times where declared to discriminate and 459 where declared inert,
with all eight load-bearing droppings reporting where declared. The population at seed 20260904 is a
different population now -- the draw is taken on every dispatched interaction, and every draw after
the first moves -- so the deep run over the branch is a fresh measurement rather than the head's
repeated.

**It was made to fail for each thing it claims before its verdict was believed**, and each is kept as
a probe. `BJ-a`: a generator that never refuses a terminal fact is refused by the required-shape
check, "No generated vector carried a terminal fact refused before the one that closes the
interaction over 100 at seed 20260904". `BJ-b`: a realization that frees the slot on the refused
fact -- the count-based defect BI1 found in the evaluator, built into the generator's own accounting
instead -- is red through `I5` and through `C4-P1`'s third clause on five vectors of the hundred,
"session s2 held 4 nonterminal interactions against its own established bound of 3". `BJ-c`: an
evaluator that is red on a refused terminal fact the design permits -- `I5` rejecting the conforming
half of its own `accepted` operand -- is red on the same five. Each was run alone over the committed
instrument and seen to return the verdict it owes. The corpus goes from 147 to 150.

What the population can and cannot see of this operand is stated rather than implied. A generated
vector is conforming, so the population catches an evaluator that is **too strict** on a refused
fact and a realization that **mis-accounts** one; it cannot catch an evaluator that is too permissive,
which stays green on every conforming input. That side is the declared mutations' --
`I5-refused-terminal-holds-slot` and `I5-terminal-closes-other-identity`, kept honest by `BI-a` and
`BI-b` -- and between the two the operand is now exercised from both sides.

## Findings

None. The frozen set reported nothing, and the instrument this pass built found nothing in the
package.

## What this pass verified rather than believed

- **The frozen set was run at `30e3f10` before any of this work existed**, in a short-path clone,
  and everything reported above was read from that run's output. The corpus took 2,170 seconds over
  22 workers and the deep properties run 857, the latter on a machine also running the coverage
  measure; the coverage measure was cut short by a session break the first time and was run again
  from the start; its figures are recorded in the commit above the one that records this pass.
- **The instrument was made to fail three ways** before its green was believed, each kept as a probe
  and each run alone over the committed instrument: `BJ-a`, `BJ-b` and `BJ-c`.
- **The shape is present at the coverage measure's count.** The properties gate at
  `-GeneratedCount 15 -SweptCount 4 -CensusPairs 1`, which is what the coverage measure runs it at,
  carries a refused terminal fact and exits 0, so the required-shape check cannot fail the measure
  by seeing too few vectors.
- **The whole set was then run over the branch**, in two further short-path clones. Over `d9dbbc0`,
  the commit that carries the instrument and its three probes and the last to touch a gate or a
  conformance declaration: the three probes alone, each returning the verdict it owes in about 33
  seconds, and the deep run at 52,000 evaluations and 0 red in 352 seconds. The corpus and the coverage
  measure are run over the head that records the pass, and their figures are recorded in the commit
  above it, as the twenty-third did.
- **The normal path is unchanged**: 26 of 26 properties, 139 evaluations over 61 declared inputs,
  9 operand mutations, and every census reporting what it reported at the head.

## What remains outside the pass

**The witness criterion is a proxy.** A field a property both names in its witness and compares in a
test that never fails reads as witness-named. Separating the two uses needs the trials to see which
statement read the value, which is the read-provenance census's method and not the operand census's.

**Both censuses still walk the declared corpus.** The twenty-third pass measured that the generated
population reaches no evaluator *conditional* the declared corpus does not, and this pass adds a
shape rather than a conditional: `I5`'s `accepted` test is reached by the declared corpus already,
and what the population adds is the value on the other side of it. A generated walk of either census
would still reach only branch bodies with no conditional of their own.

**The refused fact is one shape.** It is refused once, for an interaction the wave then closes, and
it names the identity it was claimed for. A fact refused for a mismatched identity, or refused twice,
or refused after the wave has closed -- which the contract classes as a duplicate and a protocol
fault -- is not generated, and each is a conforming shape the population does not yet carry.

**BH3 is still the owner's**, as open question 5 of the plan.

**The three narratives' finding ranges** are still read by no guard for the range.

**Cost.** The refusal is one more timeline step on one dispatched interaction in four, and nothing the
variance can distinguish at the default count; the deep run is measured above. The corpus gains three
probes, each at the default generated count with the census capped, about 33 seconds each alone.

**Whether this pass is the first of the two owed is not in question either way.** The frozen set
reported nothing and the instrument found nothing in the package, so under the 2026-09-04 ruling the
count is one. The closure review remains on hold: the ruling requires a second such pass over a
strictly larger frozen set, which this pass's three probes make available to the twenty-fifth. The
finding count by condition-4 pass is now five, six, three, two, five, one, seven, seven, five, three,
one, three, zero, three, seven, seven, three, three, five, twelve, two, five, three and **zero**.

## Where this family is dispositioned

No family is raised, so there is nothing to disposition. The pass's record is this document and the
plan's section 2aa.
