# Channel 0.2 eighth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-eighth-pass-2026-09-02-ef4b94d`

Reviewed work: the verification-foundation work done under the closure-cycle hold -- W1 (the owned
facts and their gates), W2 (the twenty-six executable properties), W3 (the status blocks and Channel
index rows), the retained guard corpus, the coverage instrument, and the AS corrections -- at
`ef4b94d`, `Merge pull request #139 from Niizuki/codex/channel-02-condition-4-seventh-pass`; raised
and corrected AT1-AT5

Date: 2026-09-02

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **eighth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it**, because it found six defects and corrected them.

## Method

The pass began from the current `origin/main`, fetched immediately before the work. It ran all **69 of
69** retained probes and the coverage gate, both green, and then took up the two surfaces the AS review
left it: the guard harness and the coverage gate, which the coverage instrument did not cover, and the
individual operands inside compound conditions, which it could not distinguish.

It did not answer either by reading. Both are now measured, and the measure is retained -- section 1.1
of the plan is about instruments rebuilt every cycle and thrown away, and this is the fourth time that
lesson has been applied rather than restated.

**Choosing the unit was the whole of the second measure.** The first attempt asked which operands never
*decided* an outcome -- never false in an `-and`, never true in an `-or`, which is the deletion test.
It reported **138 of 247** operands across the five gates, nearly all of them null checks, length
checks and `$LASTEXITCODE -eq 0` guards that are always true on well-formed input. That is the
statement-level draft the AR review discarded, one level down, and it would have been abandoned inside
a cycle for the same reason.

The unit that works is the same choice AR2 made between conditions and statements, taken one level
further: an operand the enclosing expression **evaluated around** and short-circuiting **never
reached**. It reports nine, of which four were defects, four are declared exemptions and one was
reached only by a probe. An operand whose whole expression never ran is structurally exempt, because
that is the condition measure's subject -- without that rule the facts gate's `-Apply` path is reported
a second time under another name.

## Findings

### AT1 -- `I4`'s first clause had no input that reached it -- corrected

`I4` states two clauses and named one mutation. The mutation records a possible post-dispatch loss as
`known-none`, which fires through the second; no vector in `I4`'s group carried a pre-dispatch refusal
at all, so `$certainty -ne 'known-none'` was evaluated by nothing and the first clause could be deleted
outright with both gates green.

This is **AR1 exactly**, on a property AR1's correction could not reach. AR1 closed the class "by the
gate rather than the two instances by hand", and the gate it closed it with keys on properties that
declare `conjunct` -- which `C5-P1` and `C6-P1` do and `I4` does not. **AL1's lesson in a third guise:
a guard that recognises a defect by the words the defect uses cannot see the instance that does not use
them.**

Corrected by naming both clauses in the evaluator and giving the first the input `C5-P1`'s second
clause already uses. The two clauses say the same thing about the same record, so the mutation is one
input with two memberships rather than a near-duplicate vector.

### AT2 -- two of `C6-P1`'s three obligations were unreachable -- corrected

`C6-P1`'s second clause requires a denial to record its decision point, its initiator attribution, and
`known-none`, and reads them as a disjunction of omissions. AR1 gave the clause a mutation that omits
the decision point -- the first operand -- so the expression returned there and the other two operands
were never evaluated. Each could have been deleted with every gate green.

Two inputs were needed rather than one: a record missing both obligations short-circuits at the first
and leaves the second unreached again, which is the same defect the correction is closing.

### AT3 -- `C10-P1`'s terminal-history obligation was unreachable -- corrected

`C10-P1` reads the interaction's refusal before its terminal histories and returns on the refusal. Its
one declared mutation puts the fabricated zero on the refusal, so the terminal-history obligation had no
input that reached it. Corrected with a vector that carries no refusal, which is what leaves only the
terminal history to witness it.

### AT4 -- the coverage instrument did not cover the guard harness or itself -- corrected

The inherited item. Both are now covered, and both recursion cycles are real rather than hypothetical:
this gate covers itself, and it covers the harness one of whose probes runs it. Both are broken the
same way -- every gate this file launches and every gate the harness launches is marked as nested, and a
nested run covers neither this file nor a gate declared `coverWhenNested: false`.

**Returning immediately from a nested run was the other option and it is worse**: a nested run that
measured nothing would leave every line below it reading as a line that never runs, so the measure would
report its own body. A nested run covers the design gates, which exercises the same code, and still
answers the probe that ran it.

What the condition measure then found in those two gates is **nothing** beyond four constructs that are
correctly unreachable on a passing run: the harness's no-probe message, and three branches of this gate
that a green, non-`-Report` run does not enter. Those are declared.

### AT5 -- a probe mutation survived an interrupted run -- corrected

Killing the coverage gate mid-run left one probe's heading rename in the disposition index, and the
next commit picked it up. **AS7 hardened restoration against a transient `IOException`; this is the
other way a mutation survives -- the process does not reach its `finally` block at all -- and no retry
closes it.** The harness's own residual check is what names it, and the fix is the discipline it
already states: run the harness on a clean tree and read `git status` after.

### AT6 -- every probe aimed at the coverage gate was answered by its dirty-tree refusal -- corrected

The coverage gate refuses to measure a repository with uncommitted changes, and the reason it gives is
about design artifacts: the review-target pin skips itself while one is uncommitted, so those checks
would read as checks that never run. **The refusal was over any dirty path and the reason covers one
directory.**

The difference is not academic, because the guard harness mutates a file before running the gate a
probe names. So a probe pointed at this gate was answered by the refusal rather than by the rule it
claims to test. **`AR2-a` has been green that way since AR2**, and the three probes AT4 added inherited
it before they were ever run -- written against a rule, and satisfied by something else.

That is **AO1's class from the other end**. AO1 was a guard that could not be reached by a conforming
input; this is a probe that could not reach its guard. Both are green for a reason nobody chose, and
neither is visible from the verdict.

Reproduced by hand before the fix: mutating the `AR2-a` anchor and running the gate reports the
uncommitted-changes message and never mentions the stale exemption. After scoping the refusal to
`docs/future/channel`, the same mutation reports the stale anchor, which is what the probe claims.

The scope is the directory rather than the eleven artifact names, because a list of today's artifacts
is **AN2**. A retained probe pins what is left: a design artifact with uncommitted changes still
refuses.

## What this pass verified rather than believed

- The four AT1-AT3 operands were reported red by the coverage gate before their corrections and are
  green after; the gate was run in verify mode against a committed tree, not only in `-Report`.
- The three new probes were added before the rules they probe were relied on, and each returns the
  `fail` verdict its guard owes.
- Two retained probes rotted against this pass's own edits -- AO2-a on the restated evaluation count,
  AR1-a on an anchor AT1 made match two mutations -- and the harness reported both rather than passing.
  AR1-a is re-anchored on the conjunct it is about rather than on an occurrence number, which would rot
  again the next time the order changed.
- AT6 was reproduced before and after its fix, and the message changed from the refusal to the rule.
  Its four beneficiary probes were re-run and still return `fail`, now for their own reason.
- The instrumented copy is checked to keep the gate's line count, because the design verifier measures
  its own length; a copy that grew would fail that check rather than measure it.
- The instrumented run's exit code is compared with the uninstrumented one, so a verdict that changes
  under instrumentation is reported as a defect in the measure rather than in the gate.

## What remains outside the pass

The operand measure covers the three design gates and not the guard harness or this gate, whose
operands were measured by hand for this pass and not retained: measuring them needs an instrumented
copy in `build/`, and the coverage gate's own clean-tree refusal and self-recursion make that a separate
problem from the one this pass solved. By hand, neither holds an operand of the reported class.

The measure finds an operand that is never evaluated. It does not find one that is evaluated, always
takes the same value, and could be deleted without changing any observed verdict -- **124 of the 247**
are in that class, and separating a defensive null check from a second semantic obligation inside it is
the next pass's problem. `C2-P1` and `C9-P1` each state two clauses against one mutation and are not
reported here, which is where that limit bites first.

The gate's cost is now roughly four times what it was, because covering the harness means running it.
That is the trade this measure asks for and it is stated rather than absorbed.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, **six**; a pass with findings cannot satisfy condition 4 even when all findings
are corrected.

## Where this family is dispositioned

AT is a `verification` family in its AT4 and AT5 halves and a `design` family in AT1-AT3, which reach
the completeness review's per-capability audit rows. Under the 2026-08-20 routing ruling the
verification half is dispositioned in the verification foundation plan and the design half in the
completeness review; the review policy's provenance table declares it on both axes.
