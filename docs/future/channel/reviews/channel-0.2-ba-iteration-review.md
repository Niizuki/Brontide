# Channel 0.2 fifteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-fifteenth-pass-2026-09-05-32861c6`

Reviewed work: what every consumer in the Channel 0.2 verification gates does with what its producer
hands back, at `32861c6`,
`Merge pull request #148 from Niizuki/channel-0.2-condition-4-fourteenth-pass`

Date: 2026-09-05

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **fifteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires before the closure cycle resumes.

**It is not clean.** Its frozen set reported nothing. The instrument it built found **BA1**, **BA2**,
**BA3** and **BA4** in the package on its first run, all in the retained verification and none in the
design, so the ruling's second test is not met and the two-consecutive count stays at zero. It raises
**BA5** and **BA6** against the instrument itself, found before the instrument was believed.

The method was not chosen by this pass. The fourteenth's own finding set it: AZ1 was a whole return
channel with a consumer that ignored it, and the question that finds that class — *what does each
consumer do with each thing its producer hands back, and which of those has no consumer at all* — had
never been asked of these files.

## Section numbering

**BA1**-**BA4** are the package findings. **BA5** and **BA6** are defects in the census this pass
built, found and corrected before its result was believed; under the 2026-09-04 ruling they belong to
neither counted population, and they are numbered rather than left in prose because the corrections
cite them.

## The frozen set, run first

**Zero.** The design gate, the owned-fact gate, the coverage measure, the whole probe corpus, the
declared property corpus, the 2,000-vector generated run and AZ3's dropped-field sweep were all green
before any of this pass's work existed. Ninth consecutive clean frozen set, and strictly larger than
the fourteenth's: it gained AZ3's sweep and the five probes that pass added.

It is reported with the caveat the fourteenth pass made unavoidable and this one extends: **the frozen
set could not have reported the class below.** Every instrument here asks whether a check runs, whether
a guard fires on its own subject, or whether a property is red where it should be green. A value that
is returned and never read runs perfectly, fires nothing, and is green.

### BA1 — a composed evaluator destroys its delegate's evaluation errors

Three of the twenty-six properties evaluate a clause by calling another property's evaluator.
`Invoke-C4P1` delegates its second and third clauses to `Invoke-I1` and `Invoke-I5`; `Invoke-C2P1`
delegates its first and third to `Invoke-S1` and `Invoke-S4`; `Invoke-C8P1` delegates both of its
clauses to `Invoke-I2` and `Invoke-I3`.

Each read the delegate's `Verdict` and, on red, its `Witness`. Each then returned a record built by
`New-Red` or `New-Green` — **which construct a fresh, empty `Errors` collection.**

So the delegate's `Errors` were not merely unread at those six sites. They were **destroyed**. That is
AZ1 one level below the loop AZ1 was raised against, and worse in kind: a loop that does not drain a
channel can be made to drain it afterwards, and a producer that rebuilds the channel empty has thrown
the contents away before any consumer could reach them.

It was demonstrated rather than argued. `Invoke-I1` was made to report an evaluation error on every
input, and the gate run twice:

- **before the correction, four reports** — `I1`'s own four declared inputs, and nothing from `C4-P1`;
- **after it, eight** — the same four, plus four of `C4-P1`'s six, which are the inputs on which
  `C4-P1` reaches its delegate at all.

The declared corpus runs thirty-four such delegated evaluations and the generated population adds six
hundred more. Every one of them discarded whatever its delegate reported through that channel.

Nothing fills `Errors` today except `Invoke-C4P2`, and `C4-P2` is delegated to by nothing, so no
information was actually lost at the pin. That is what makes this a latent defect rather than a live
one, and it is exactly the state AZ1 was in for the eight cycles before the run that exposed it.

**Corrected.** `New-Red` and `New-Green` take `-Inherited`, and each of the six sites reads all four
members the delegate hands back — the verdict, the witness, the conjunct its red arrived through, and
the errors. The conjunct is inert until a delegate names one and is read now so that a delegate which
starts naming them does not lose it silently.

### BA2 — the operand-mutation harness read the verdict and nothing else

The gate dispatches an evaluator at five top-level sites. The declared-input loop drains `Errors`,
compares the verdict, prints the witness, and checks the conjunct. AZ3's sweep does all four. The
generated loop does three of the four after AZ1's correction.

**The two dispatches in the operand-mutation harness did one.** Both read `Verdict` alone.

Three things follow, and each is a rule already stated and enforced somewhere else in the same file:

- **`Errors` is discarded at both.** A drop that removes a whole frame reference rather than one of
  its fields leaves the record unevaluable, and an unevaluable record comes back `green`, because a
  record the property could not read produces no witness. Five of the nine declared operand mutations
  are declared green. Demonstrated: an operand mutation dropping `unseen-refusals.refusedFrame` on
  `C4-two-sessions-one-identity` and declaring it green was **accepted**, and the gate reported ten
  operand mutations and passed. That is AZ1's failure, at the harness AZ1's own correction did not
  reach — because that correction went to the loop the finding was found in and the other two
  dispatches were never enumerated.
- **The conjunct is not checked.** Four of the nine mutations are declared to leave the property red.
  The declared-input loop requires a red to arrive through the conjunct its mutation is declared
  against, and AZ3's sweep requires the same of a dropped field, both giving the same reason: a red
  arriving through the other conjunct witnesses something other than the operand it names. This was
  the third place a declared red is judged and the only one not asking.
- **The witness is dropped** from the published-form mismatch, where both the other verdict
  comparisons in the file carry it.

**Corrected.** Both dispatches drain `Errors`. Operand mutations declaring `red` now declare the
conjunct, which is required rather than optional, and the four that do were each read off the
evaluator rather than reasoned about: `AK1-session-on-refused-frame` and
`AK5-arrival-ordinal-on-refused-frame` fire through conjunct 1, `AK6-terminal-frame-to-form-only-outcome-precedes-ack`
and `Y4-and-AK6-together` through conjunct 2. The published-form message carries the witness and the
conjunct.

### BA3 — the accumulator was cleared at two of the five dispatches

`Errors` is not the only channel an evaluation reports through. `Read-Required` adds to
`$script:UnpublishedFields` when an obligation reads a field the vector does not publish, and **AU2**'s
rule is that a verdict produced by an input's silence is evidence of neither conformance nor violation.

The declared-input loop clears that collection before its dispatch and drains it after. The generated
loop does the same. **The two operand dispatches and the sweep's dispatch did neither.**

The operand harness is the one place in this file that builds an input **by removing fields**, which
is precisely the condition the accumulator reports on, and it was the one place not listening. It is
latent for a second reason as well as the first: `C4-P2` is the only property with operand mutations
and its evaluator reads through `Get-Field` rather than `Read-Required`. Both of those are facts about
today's members, not about the rule.

**Corrected** at all three, and the rule is now over the dispatch rather than over which evaluator
happens to sit behind it.

### BA4 — the generated loop never read the conjunct

The one member of an evaluator's record the generated loop still dropped after AZ1. It declares no
expected conjunct there — every red on a conforming vector is a failure whichever clause produces it —
so this is diagnosis rather than a check, and it is the difference between a reported counterexample a
reader can act on and one they have to reproduce by re-running the seed.

**Corrected**: a generated red names the conjunct it arrived through. It is recorded as a finding
rather than folded into the others because the census reported it as the same shape and nothing about
its size changes that.

### BA5 — the census reported two of seven producers while looking total

The first run of the instrument reported the coverage gate's `Get-EvaluatedOperand` and the properties
gate's `Invoke-C4P2` and nothing else. `Get-ExecutedLines`, `Read-Text`, `New-Red`, `New-Green` and
`New-ConformingVector` were all invisible, and every one of them has exactly **one** record shape.

The cause is PowerShell's own: a function returning a single-element `List` unrolls it, so the caller's
`.Count` guard saw a scalar and skipped it. A measure that reports a subset while its output reads as
a total is the thing this whole programme is about, and it happened in the instrument built to find it.

**Corrected** by comma-wrapping the return, which is stated at the return rather than at the call so
the next reader of that function sees why.

### BA6 — BA1's own correction made the census blind to BA1's own subject

The census recognised a producer by a record literal in the return position. **BA1's correction gave
`New-Red` and `New-Green` a body that builds the record into a variable and returns the variable**, and
the census went from twenty-nine producers to two in the properties gate. The six composed evaluators
stopped being producers, so their consumers stopped being consumers, and the run went green over a
package it was no longer looking at.

This is **AP1**'s class — a key correct when written and expired when the work moved — arriving inside
the commit that introduced the key, which is as short as that interval gets. It is also the argument
against the whole shape of the first draft: a measure that recognises a thing by the syntax someone
happened to write is a measure of that syntax.

**Corrected** by resolving one level of indirection — what a function hands back includes what was
assigned to the variable it returns. The depth is a stated limit and not a claim: a producer reaching
its record through two variables is not censused.

## The instrument, and what it reports

`build/verify-channel-0.2-return-channels.ps1`, declared by
`conformance/channel-0.2-return-channels.json`, and in the repository gate rather than behind the
gate-self-check switch: it parses rather than executes, so a run costs about a second.

It has two units.

**Returned members.** A producer is a function that hands back a record — a hashtable or
`[pscustomobject]` literal, the record assigned to the variable it returns, or another producer's
result, taken to a fixed point. A consumer is an assignment whose right-hand side calls one, directly
or through `& $variable` where the file builds a dispatch table of `${function:...}` references. Every
member the producer can return must be read from the assigned variable in the consumer's own scope.
**Thirty-one producers and fourteen consumers across the five gates**, one declared producer exemption
and no member exemptions.

**Script-scope accumulators.** Every `$script:` collection any function adds to must be declared, as
`per-evaluation` — cleared before every top-level dispatch and drained after it — or as `cumulative`,
read once over the run and never cleared. Two are declared: `UnpublishedFields` and
`ObligationsReached`. A new one that nobody declares is a failure, which is what makes this unit total
rather than a list of the two that exist today.

Run against the package as this pass found it, it reports **twenty-one** findings: the twelve of BA1,
the five of BA2, the three of BA3, and BA4.

**Under the 2026-09-04 ruling this instrument is quarantined** and joins the frozen set at the
sixteenth pass.

## Findings

Four against the package — **BA1**, **BA2**, **BA3** and **BA4** — all in the retained verification and
none in the design. Two against this pass's own instrument, **BA5** and **BA6**. All six are corrected
in this pass.

The frozen set found **nothing**, for the ninth consecutive pass.

## What this pass verified rather than believed

- **The frozen set was run first**, whole, before any of this work existed.
- **BA1 was pinned by injection, not by reading.** The same injected evaluation error produced four
  reports before the correction and eight after it.
- **BA2's `Errors` half was pinned by a mutation that passed.** Declaring an unevaluable operand
  mutation green was accepted by the gate as it stood.
- **Each of the four red operand mutations' conjuncts was read off the evaluator** rather than
  inferred from the finding it was raised for.
- **Every new check was driven red once.** An unevaluable mutation declared green; a red mutation
  declared against the other conjunct; a red mutation declaring no conjunct; an obligation reading a
  field the mutated vector does not publish, which fires at both operand dispatches; and a generated
  red, whose message now names its conjunct.
- **Eleven probes were added and each returns the verdict its guard owes**, including one `pass` probe
  that exercises the member-exemption path no clean run reaches.
- **BA6 was found by re-running the census after the correction rather than by trusting it.** The pass
  that did not re-run it would have reported a clean census over a package the census had stopped
  reading.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs, nine
  operand mutations, 2,600 generated evaluations at 0 red, eighteen droppings with eight load-bearing.

## What remains outside the pass

**The census is a floor and says so.** Three limits are stated in the file rather than discovered
later:

- a read is counted in the consumer's scope, not on the path that needs it, so a consumer reading a
  member on one branch and dropping it on another reads it. Measured: removing the delegate's `Errors`
  from `C4-P1`'s red path alone does not fire, because its green path still reads them. Separating
  those needs path analysis and a different instrument;
- a record passed **whole** to another function is not a read of its members, which is why the
  corrections here read each member at the call site instead of forwarding the record; and
- a producer reaching its record through two variables is outside BA6's one-level resolution.

**Nothing yet requires an evaluator to FILL `Errors` where a record is genuinely unevaluable.** The
plan named this as the second candidate and it is untouched: one evaluator of twenty-six populates
that channel, and whether the other twenty-five have unevaluable records they silently call green is
the AU1-shaped question one level in. This pass made the channel reach its consumers; it did not ask
whether the producers use it.

**The census gate is not itself under the coverage measure.** Its own exemption-matching branches are
reached by probes and by no clean run, so registering it needs coverage exemptions written with the
reasons — the judgement is left to the pass that inherits it under quarantine rather than made by the
pass that wrote the code.

**No timing measure was re-taken.** Another process was running throughout on this machine, and the
plan's section 4 records what a contended measurement was worth the last time one was believed.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, **four**.

## Where this family is dispositioned

**BA1** through **BA6** are corrections to the verification instruments and their declarations, not to
the design, so under the 2026-08-20 ruling they belong in the verification foundation plan's own record
and not in the completeness review's disposition index. The plan's section 2r carries them, and this
document is the pass's evidence.
