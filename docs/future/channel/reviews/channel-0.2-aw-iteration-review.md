# Channel 0.2 eleventh W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-eleventh-pass-2026-09-04-07b4884`

Reviewed work: the Channel 0.2 design's own claims, through the twenty-six executable properties, at
`07b4884`, `Merge pull request #143 from Niizuki/channel-0.2-condition-4-tenth-pass`; raised and
corrected AW1, and reports AW2 to the owner

Date: 2026-09-04

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **eleventh** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and the first run under the 2026-09-04 ruling that re-scoped how that condition is counted.

## The frozen set, run first and before anything was built

Under that ruling a pass runs the instruments as they stood at its start and records what they report
as its **package findings**, before it builds anything.

**Zero.** `build/verify-gate-self-checks.ps1` reported 77 of 77 probes green and the coverage measure
green. That is the fifth consecutive pass whose frozen set has found nothing.

## The instrument this pass built

By owner decision of 2026-09-04 the method is **generated vectors run against the twenty-six
executable properties**, because every property is checked against hand-authored vectors with
hand-chosen mutations and no pass since closure review 16 has examined the design's own claims.

The generator builds each vector to satisfy the design's stated rules by construction: transitions are
drawn only from the legal table the properties gate already cross-checks against the session state
machine, interactions dispatch only from `established`, admission stops at the session's first drain,
and admitted interactions run in waves that reach the session's established bound and never pass it.
A property red on such a vector is red on conforming behaviour, which is AE1's class.

**Result: 0 red over 2,000 generated conforming vectors — 52,000 property evaluations — at seed
20260904.** The same run is green at every count tried between 15 and 2,000.

**What that covers.** The population carries one, two and three sessions in roughly equal measure;
0 to 11 interactions; sessions that fault and sessions that drain and close; both routes to
`established`; sessions carrying no interaction at all; and interaction identities **reused across
sessions**, which is the case AH1's multi-session ruling and the AK7, AK8 and AL1 session-scoping
corrections exist for. Those corrections hold over roughly 1,300 multi-session vectors.

**Why the number is trustworthy, which matters more than the number.** A generator that cannot produce
a violation reports the same green over any population, so it was falsified before it was believed.
Six deliberate violations were injected into the generator and each was caught by the property that
owns the rule:

| Violation injected into the generator | Properties that went red |
| --- | --- |
| dispatch while draining | `S2`, `S3` |
| two terminal histories on one interaction | `I2`, `C8-P1` |
| a cancellation acknowledgement recorded as a semantic success | `I3`, `C8-P1` |
| a session resuming after a terminal state | `S4`, `C2-P1` |
| a wave running one past the established bound | `I5`, `C4-P1` |

The bound is therefore exercised from both sides: reached from the legal side on a large share of the
population and exceeded by one in the mutation, which is where a comparison written with the wrong
operator would show itself.

**Two defects in the generator were found and fixed before it was trusted**, and they are recorded
because they are what the falsification was for rather than findings against the design. `1..0` counts
**down** in PowerShell and yields `1,0`, so a range as a possibly-empty sequence produced an
interaction named `i0` and made a session with no interactions impossible; and closing a wave emitted
a terminal form that the interaction's own record did not carry, which would have made the vector
incoherent and produced a finding about the generator wearing the shape of a finding about the design.

**The generator asserts its own required shapes** — a faulting session, an `establishing` route, a
drained close, a session with no interaction, a vector with more than one session, and a wave that
fills the bound — and fails when the population has lost one. A rate is worth what its inputs cover,
and a generator that quietly narrowed would otherwise report the same large green number.

It runs on **every commit** at a hundred vectors, which costs seven tenths of a second against this
gate's one second; `verify-gate-self-checks.ps1` raises it to two thousand. The cost is superlinear —
100 costs 0.7s, 500 costs 5.7s, 2,000 costs 47s — so the count is a dial rather than something to
maximise, and tracing the generated block is why the coverage gate now runs this gate at a declared
count of fifteen.

## Findings

### AW1 -- the machinery forbade recording a pass that found nothing -- corrected

A retained iteration review must record at least one finding, on the stated ground that "a retained
iteration review exists to record findings, so parsing none from one is the defect". That was true of
the ten passes that had run.

The 2026-09-04 ruling made "the frozen set reports nothing and a newly built instrument finds nothing
in the package" the outcome condition 4 asks for, and the two-kinds-of-review section still requires
such a pass to be retained as evidence. So the guard forbade recording the one result the programme is
working toward — **AP1's class**, a key that was correct when written and expired when the work moved,
and it expired one day after the ruling that moved it.

**Corrected**, and the pattern stays falsifiable, which is what that check is for. A review that parses
no finding must state `This pass records no finding against the package.`, and a review that states it
must carry no finding heading at all. A heading pattern that has quietly stopped matching still fails,
because nobody writes that sentence into a review that found things.

## AW2 -- reported to the owner rather than corrected

**The 2026-09-04 ruling partitions a pass's findings into two populations, and AW1 is in neither.**
Package findings are what the frozen set reports; first-run findings are what a newly built instrument
reports. AW1 came from neither. It was found by reading, while trying to record this pass's result.

Both of the ruling's tests pass for this pass: the frozen set reported nothing, and the instrument this
pass built found nothing in the package. Under the previous reading — "an author-side pass that finds
nothing it can fix" — this pass fails, because AW1 was a defect and it was fixed.

Which reading governs is an owner decision and is not taken here. Recorded so the twelfth pass is not
the one to discover that the ruling has a third case, and so the count is not quietly decided by
whoever writes the next section heading.

## What this pass verified rather than believed

- **The frozen set was run first and reported nothing**, before any of this pass's work existed.
- **The generator can fail.** Six injected violations, each caught by the property that owns the rule.
- **The bound is reached from the legal side** and exceeded by one in a mutation, so `I5` and
  `C4-P1`'s third clause are evaluated at their boundary rather than only far from it.
- **The population has the variety the number depends on**, asserted by the generator on every run
  rather than measured once and trusted.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs, 9
  operand mutations, exactly as before.

## What remains outside the pass

**The generator does not produce refusals, and that is the largest gap.** No pre-dispatch refusal, no
recorded `unseen` refusal, no late-traffic latch, and no declared stimulus steps — so `C4-P2`'s two
conjuncts are evaluated over empty observation records and are vacuously green, `I4`'s first clause and
`C5-P1`'s second are unexercised, and `C10-P1`'s certainty clauses are reached only through the
declared corpus. The property eight finding families have been about is the one this instrument reaches
least, and extending the generator to refusals is the obvious next increment.

**It generates conforming vectors only.** The mutation direction was used to falsify the generator by
hand and is not retained; a retained mutation mode would give the falsifiability rate the same
treatment as the soundness rate.

**Tracing the generated block is expensive**, which is why the coverage gate runs this gate at fifteen
vectors rather than the default hundred, and why the deep run lives behind the self-checks switch.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, **one** — and under the 2026-09-04 ruling the number that
matters is the package finding count, which is zero.

## Where this family is dispositioned

AW is a `verification` family. AW1 is in the design verifier's handling of retained review records and
AW2 is about the ruling in the verification foundation plan; neither reaches a design artifact, so the
disposition belongs in that plan.
