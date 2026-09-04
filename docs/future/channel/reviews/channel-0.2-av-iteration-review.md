# Channel 0.2 tenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-tenth-pass-2026-09-04-7798db4`

Reviewed work: the verification-foundation work done under the closure-cycle hold -- W1 (the owned
facts and their gates), W2 (the twenty-six executable properties), W3 (the status blocks and Channel
index rows), the retained guard corpus, the coverage instrument, and the AU corrections -- at
`7798db4`, `Merge pull request #142 from Niizuki/channel-0.2-condition-4-ninth-pass`; raised and
corrected AV1-AV2

Date: 2026-09-04

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **tenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it**, because it found two defects and corrected them.

## Method

The pass began from `origin/main`, fetched immediately before the work, and started by running the
retained instruments as the AU review asks: **77 of 77** probes green and the coverage gate green.
Neither had rotted.

Its inherited brief was the one the AU review left. The obligation measure works because the property
gate routes every verdict through one constructor; the design, facts and guard gates raise
`$failures.Add` from hundreds of sites with no equivalent chokepoint, so a guard there that runs and
cannot fail is found only by the probe corpus, one guard at a time. The open question was whether
those sites can be given a comparable unit.

**They can, and the answer is the same trick one level over.** `$failures.Add` *is* the chokepoint --
it is the one place those gates state a finding. The obstacle is that a passing run of a design gate
produces no findings at all, so measuring which sites a passing run reaches is vacuous. The inputs
that make those guards fire are the probes. So the measure is: **which guard sites does the corpus
reach**, and the instrument is the corpus with the failure constructor recorded.

That measurement is reported below. What it produced along the way is both findings, and neither came
from the number.

## Findings

### AV1 -- a probe asserted the exit code, not its own guard -- corrected

The corpus's stated contract is that "a probe makes ONE guard's own subject present in the package and
asserts the verdict that guard must return", and its failure message is "a guard that no longer
answers its own subject is a guard that has stopped measuring". What it actually asserted was the
gate's **exit code**, which is a whole-gate verdict. Every `expect: fail` probe therefore passed
whenever the gate failed, for any reason at all.

**How it surfaced.** Building the measurement above needed an instrumented copy of each gate. The
first attempt prepended the recorder, which pushed `param()` out of first position and made three of
the five gates fail to **parse**. The corpus reported **77 of 77 probes returned the verdict their
guard owes** while three gates were not running at all. That is the defect stated as plainly as it can
be: the probes could not tell a guard firing from a gate that never started.

**What the evidence does and does not show.** Adding an unconditional unrelated failure to the design
gate leaves 76 of 77 probes green, and the one that notices is the single probe on that gate expecting
a pass -- but in that scenario the probes' own guards *do* still fire, so passing is not wrong, only
uninformative. The decisive test is the one where the guard stops firing and the gate still fails:
`AP1-a`'s guard was silenced and an unrelated failure added, and the probe now **fails** for the right
reason, where the exit-code comparison it replaced could only have passed. That is the case AV2 turned
out to be a live instance of.

**Corrected.** Every `expect: fail` probe now declares a `guardMessage`, and the harness requires it in
the gate's output. Two details are load-bearing and were each found by being wrong first: the output is
taken from each record rather than through `Out-String`, which renders at the console width and cut
every `Write-Error` message in half; and both sides are compared with **whitespace removed** rather
than collapsed, because a child process wraps its own output mid-word -- `Session-Stat e-Machine` -- so
no amount of collapsing makes the message match. The field is **mandatory**, not optional: an optional
one would leave part of the corpus asserting the exit code while the count reported a whole number.

Sixty-six of the fragments are literal runs from the guard's own source, checked to occur in exactly
one guard message across the gates; the remaining eight guards state messages that are almost entirely
interpolated, and those probes take a fragment of the specific text the guard produces for them.

### AV2 -- a guard in the design gate could not fire, and its probe was green on the crash -- corrected

The first run of the AV1 check found one probe that could not be given a `guardMessage` at all:
**AN3-b**, whose gate failed while printing no guard message of any kind.

Every git call in the design gate redirects stderr with `2>$null`. In Windows PowerShell that
redirection wraps each stderr line in an `ErrorRecord`, and the gate's own `$ErrorActionPreference =
'Stop'` turns the first one into a **terminating error at the call**. So a git invocation that fails
does not hand a non-zero `$LASTEXITCODE` to the check written to read it -- it kills the gate where it
stands.

The guard AN3-b names is the one that reports a historical measure naming a commit the verifier cannot
be read at. It sits eight lines below the `git show` that dies, so it **could never fire**, and every
check after that point in a 2,700-line gate was skipped as well. The probe had been green on the crash
since it was written.

That is **AO1's class** -- a guard no input can reach -- one level below where AO1 found it, and it is
exactly the class the tenth pass was briefed to look for. It was invisible to the coverage instrument,
because the guard's condition *is* evaluated on a passing run and simply never true, and invisible to
the corpus, because the probe read only the exit code.

**Corrected** as a class rather than as the instance: all seven git calls route through one
`Invoke-Git` that lowers the preference for the duration of the call, so the next call added does not
reintroduce it. With the fix in place the guard fires with the message it was written to produce, and
AN3-b is anchored on it.

## What this pass measured

**The corpus reaches 62 of the 299 guard sites in the five gates — 21%.** By gate: design 29 of 209,
properties 16 of 51, facts 11 of 17, coverage 6 of 11, and the guard harness 0 of 11, which no probe
names because it is the runner. The measure records each `$failures.Add` call site the corpus makes
fire, by running the corpus against instrumented copies that behave identically to the gates -- 77 of
77 reproduced through them.

The number is a floor on how much of the guard population is measured, not a defect count. What it is
useful for is the same thing the coverage measure is useful for: it names the 237 guards that nothing
currently makes fire, and AV2 is what one of them turned out to be hiding.

**The instrument is not retained.** It needs an instrumented copy of each gate, and keeping copies in
`build/` is the problem AT4 already declined to solve for the same reason. What is retained instead is
the `guardMessage` requirement, which is the part of it that pays every run: a probe that stops
reaching its guard now says so.

## What this pass verified rather than believed

- **The retained probes still apply.** 77 of 77 before anything was touched.
- **The instrumented copies behave as the gates do.** 77 of 77 reproduced through them, which is what
  makes the 21% a measurement of the corpus rather than of the instrumentation.
- **The AV1 check is load-bearing.** Silencing `AP1-a`'s own guard while leaving the gate failing takes
  the probe red; it passed before.
- **The AV2 guard now fires**, with its own message, on the input AN3-b applies.
- **The gates are unchanged in verdict.** Design, facts, properties and coverage all pass, and the
  corpus is 77 of 77 with every fail-probe now asserting its guard.

## What remains outside the pass

**The 21% is not raised.** Writing probes for 237 guards is not this pass's scope and would be a poor
use of one: the corpus's value is in the guards someone had a reason to doubt. What the number is for
is the next pass's judgement about *which* of them to reach, and the honest reading is that a low
number here is not by itself a defect.

**A probe still cannot say that its guard was the only one that fired.** `guardMessage` asserts
presence, not exclusivity, and a probe whose edit trips a second guard as well is indistinguishable
from one that trips only its own. Sixteen probes fire more than one guard site today.

**The `Stop`-preference hazard was closed for the design gate's git calls and not audited across the
other gates' native-command calls.** The class is closed where it was proven; whether the facts,
properties, coverage or harness gates hold the same shape is unexamined.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, **two**; a pass with findings cannot satisfy condition 4 even when
all findings are corrected.

## Where this family is dispositioned

AV is a `verification` family. Both findings are in the gates and the probe corpus, and no correction
reaches any design artifact, so under the 2026-08-20 routing ruling its disposition belongs in the
verification foundation plan rather than in the completeness review.
