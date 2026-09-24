# Channel 0.2 twenty-fifth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-5-channel-0.2-condition-4-twenty-fifth-pass-2026-09-24-cad30af`

Reviewed work: the refused terminal facts the twenty-fourth pass gave the generated population, given
every identity shape the capability contract's failure clause names rather than the one they had, so
that the `accepted` tests of `I7` and of `C4-P1`'s first clause are exercised from the conforming side
as `I5`'s already was, at `cad30af`, `Merge pull request #160 from
Niizuki/claude/condition-4-twenty-fourth-pass`

Date: 2026-09-24

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twenty-fifth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **second of the two consecutive clean passes** the 2026-09-04 ruling
requires.

**It is the second of the two.** The frozen set reported nothing at `cad30af` -- the fifth
consecutive clean frozen set, and a strictly larger one than the twenty-fourth's, having gained that
pass's three probes -- and the instrument this pass built found nothing in the package on its first
run. Under the ruling's two populations that is two consecutive clean passes, the second over a
strictly larger frozen set, which is condition 4 as the ruling states it. **BK1** is the pass's own
new code -- the second form it gave the design gate's next-pass check -- caught by the frozen coverage
measure when the whole set was run over the branch, and corrected before the pass reported; it is in
neither of the ruling's two populations, being neither in the package as the pass found it nor a
first-run finding of the instrument, which is the reading this review takes and states rather than
assumes, since the owner may rule otherwise. **Meeting the condition does not lift the hold**: the
owner ruled on 2026-09-24, in the plan's section 6, that lifting it stays a separate owner decision,
and the closure-cycle state and the review policy's do-not-dispatch marker are left as they were.

## The frozen set, run first

**Zero.** In a short-path clone at `cad30af`, before any of this pass's work existed: the design
gate, the owned-fact gate, the return-channel census, the properties gate at its default count --
26 of 26 properties over 61 declared inputs, the read-provenance census over both polarities at 0 raw,
the field-readership census at 0 read by nothing, the per-property operand census at 167 pairs and 0
inert, the operand census at 84 fields tried and 0 inert and undeclared, the closed-vocabulary census
over both populations, the declaration-polarity and declaration-citation checks, and 2,600
evaluations over 100 generated vectors at 0 red -- and then the gate self-checks: the 150-probe corpus
at 150 of 150 over 16 workers, the coverage measure over four gates with the 47 condition and 6
operand exemptions it declared before this pass, and the deep run at 52,000 evaluations over 2,000
generated conforming vectors at 0 red, AZ3's dropped-field sweep over 500 of them evaluated 8,541
times where declared to discriminate and 459 where declared inert, all eight load-bearing droppings
reporting where declared. All exit 0. The head is the twenty-fourth pass's merge, and nothing merged
above it.

## The instrument

The twenty-fourth pass gave one dispatched interaction in four a terminal fact that is refused before
the one that closes it, and every such fact closed its own identity. That exercised `I5`'s `accepted`
test from the conforming side and no other evaluator's. `I7` and `C4-P1`'s first clause read the same
field and skip a refused fact before reading what it closes -- and a refused fact closing exactly its
own identity is one each of them would have passed with the skip gone: `I7` asks whether a terminal
fact closed an identity other than its own, and `C4-P1` whether it closed exactly one. So the
conforming half of both tests was exercised by nothing, in the declared corpus or the population.

**That was measured at the head rather than inferred.** With `I7`'s `accepted` test deleted, the
properties gate at `cad30af` passed every check it runs, the uncapped per-property census included:
once the test is gone `I7` no longer reads the field, so no census has anything to measure. With
`C4-P1`'s deleted, the gate failed only through the uncapped per-property census -- `C4-P1` still reads
`accepted` through the `I5` delegate, and it no longer decided anything for it -- and passed under the
one-pair cap every probe in the corpus runs it at.

The twenty-fourth named the shapes that would reach both. A refused fact now closes its own identity,
a **mismatched** one -- a live sibling where the wave holds one, otherwise an identity its session
never admits, which another session may, so the contract's wrong-session case is inside it -- an
**extra** one beside its own, or **none**. Those are the contract's failure clause for terminal facts,
"missing, extra, wrong-session, or mismatched identities reject the claimed terminal fact," each
applied to a dispatched interaction that stays nonterminal. And one refused claim in four is followed
by a **second** before the fact that closes the interaction, since an interaction is as nonterminal
after two rejected claims as after one. Each shape is required of the population, keyed on what makes
a skipped test decide something: one identity other than its own is what `I7` would call a changed
sibling, and none or two is what `C4-P1` would count.

**The shapes are drawn from a stream of their own.** A draw on the generator's stream moves every
draw after it, which is why the twenty-fourth's draw made the population a different one. Drawn apart,
the population at seed 20260904 is the one it was at the head, carrying the new shapes: every probe
anchored on a generated witness still reads the witness it recorded, and `BJ-b`, whose edit belongs to
a line this pass rewrote, is re-anchored on that line and returns the witness it recorded before.

**On its first run over the package as found it reported nothing**: 0 red over 100 generated
conforming vectors, 26 of 26 properties, with every required shape present, and the shapes present at
the coverage measure's count of 15 as well, so the required-shape check cannot fail that measure by
seeing too few vectors. Over the deep run it reported nothing either: 52,000 evaluations over 2,000
generated conforming vectors at 0 red, and the sweep's figures identical to the head's -- 8,541 and
459 -- which is the separate stream shown rather than asserted, since a population that had moved
would have moved them.

**It was made to fail for each thing it claims before its verdict was believed**, and each is kept as
a probe. `BK-a`: a generator whose refused facts go back to closing their own identity is refused by
the required-shape check for all three new shapes. `BK-b`: `C4-P1` with its `accepted` test deleted is
red on generated conforming vectors, "an accepted terminal fact in session s2 closes 0 admitted
interactions", on facts closing none and facts closing two. `BK-c`: `I7` with its test deleted is red,
"the terminal fact for interaction i2 in session s1 changed sibling i1's terminal history". `BK-d`: a
generator that never refuses a second claim is refused by the required-shape check. `BK-e`: a
realization that keeps the slot across the first refused fact and frees it on the second is red through
`I5` and `C4-P1`'s third clause, which `BJ-b`, freeing it on the first, cannot tell apart from a
realization right about the first and wrong about the second. Each was run alone over the committed
instrument and seen to return the verdict it owes. With `BK-f`, which pins the design gate's second
next-pass form below, the corpus goes from 150 to 156.

What the population can and cannot see is stated rather than implied, as the twenty-fourth stated it.
A generated vector is conforming, so the population catches an evaluator **too strict** on a refused
fact of any of these shapes and a realization that **mis-accounts** one; it cannot catch one too
permissive, which stays green on every conforming input. That side stays the declared mutations':
`I7`'s and `C4-P1`'s own, which flip the verdict when `accepted` is flipped, and `I5`'s two.

## Findings

The frozen set reported nothing, and the instrument this pass built found nothing in the package. One
finding was raised in the pass's own new code.

### BK1 A conditional the design gate's new next-pass form put in a failure body

The second form this pass gave the design gate's next-pass check chose between two failure messages
with an inline `if` written inside the `$failures.Add(...)` call of the mismatch branch. A passing run
never enters that branch, so the conditional is never evaluated, and the frozen coverage measure
refused it when the whole set was run over `c2e64dd`, the commit recording the pass: "this if is never
evaluated by a passing run, so the check it guards did not run." That is BH5's shape, a conditional
only a failing run reaches, in a guard written by the pass that the measure then ran over. The message
is now chosen before the branch, where every run evaluates it, and the measure passes over the
corrected gate with the exemptions it already declared -- no exemption was added for it. The 156-probe
corpus had passed over `c2e64dd` beforehand, `BK-f` included, which is why the defect was a coverage
finding and not a behavioural one: both messages were right, and one of them was chosen in a place
nothing measured.

## What this pass verified rather than believed

- **The frozen set was run at `cad30af` before any of this work existed**, in a short-path clone,
  and everything reported above was read from that run's output. The self-checks took 5,107 seconds,
  on a machine also running this pass's own measurements in a second clone.
- **The operand's reach at the head was measured, not reasoned.** `I7` and `C4-P1` were each run with
  their `accepted` test deleted over the head without this instrument, at the uncapped census: `I7`'s
  deletion passed everything, `C4-P1`'s failed only the per-property census, and under the one-pair
  cap every probe runs at, both passed.
- **The instrument was made to fail five ways** before its green was believed, each kept as a probe
  and each run alone over the committed instrument, `BK-a` through `BK-e`, beside `BJ-a` through
  `BJ-c`, all eight returning the verdict they owe at `a215fa9`, the commit that carries the
  instrument.
- **The coverage measure over that commit** passes with the 47 condition and 6 operand exemptions
  it already declared: every construct the instrument adds is reached at the measure's count of 15.
- **The deep run over that commit** is the figure above, 52,000 evaluations at 0 red.
- **The design gate's second form is pinned by `BK-f`**, and `AX1-b` is re-anchored on the sentence
  the review policy now carries.
- **The whole set was run over `c2e64dd`**, the commit recording the pass, in a third short-path
  clone: the 156-probe corpus at 156 of 156 over 16 workers, and then the coverage measure, which
  refused the conditional that is **BK1** and stopped the self-checks before the deep run. The
  correction is in the commit above that one, and the corpus, the coverage measure and the deep run
  over the head carrying it are recorded in the commit above that.
- **The normal path is unchanged**: 26 of 26 properties, 139 evaluations over 61 declared inputs,
  9 operand mutations, and every census reporting what it reported at the head.

## What remains outside the pass

**A duplicate terminal is not generated.** A fact refused after its wave has closed is the one shape
the twenty-fourth named that this pass does not carry, and it is left for a stated reason rather than
for want of time: the contract classes it as a duplicate terminal, which attempts an
interaction-scoped `state-violation` and settles a late-traffic latch, and a latch is a frame-level
record. The generator keeps the session timeline and the frame-level records on disjoint identities
-- `i<n>` for the one, `f1`, `g<k>`, `h<k>` and `u1` for the other -- so a duplicate terminal on the
timeline would be a vector carrying a latch-owing event with no latch, which is a vector the design
does not permit and not a conforming shape. Generating it honestly needs the two views joined on one
identity, which is a change to the generator's structure rather than a shape inside it.

**The session machine's `accepted` test is the same question one record over, and it is measured
rather than guessed at.** `S1` and `S4` skip a transition the realization rejected, and no vector
carries one -- none of the 61 declared inputs and none of the generated population. With either skip
deleted the properties gate at this branch passes every check it runs, the uncapped per-property census
included, for the reason `I7`'s deletion passed at the head: the skip is the only place the property
reads the field. That is the class the coverage measure names as its own limit -- a condition that is
evaluated and cannot fail -- and it is the class this pass and the twenty-fourth each closed one
record's worth of, so it is recorded here as the next unit rather than counted as a finding against
the package: no evaluator is wrong, and what is missing is an input that would say so if one were. A
rejected transition is a conforming input the design describes, though narrowly: the session machine's
event totality rule leaves the state unchanged only for a wrong-state local action that has emitted no
frame, and only where the refused/illegal table says so -- a local drain or close before
establishment is the transition-shaped one -- while most other refused inputs fault the session. Giving the generator one of those is the same shape
of work as this pass's, over the session record.

**The witness criterion is a proxy**, and **both censuses still walk the declared corpus**, as the
twenty-fourth left them. This pass, like that one, adds values on the other side of conditionals the
declared corpus already reaches rather than a conditional.

**BH3 is still the owner's**, as open question 5 of the plan.

**The three narratives' finding ranges** are still read by no guard for the range.

**Cost.** The new draws are on a separate stream and add at most two timeline steps to one dispatched
interaction in four; nothing the variance can distinguish at the default count. The corpus gains five
probes, each at the default generated count with the census capped.

## Where this family is dispositioned

**BK** is raised against the verification -- a gate -- and no design artifact names it, so under the
2026-08-20 ruling its disposition is in the verification foundation plan's section 2ab, and the review
policy's provenance table classifies it `verification`.
