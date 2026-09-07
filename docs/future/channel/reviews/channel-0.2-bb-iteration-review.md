# Channel 0.2 sixteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-sixteenth-pass-2026-09-06-a3b22f4`

Reviewed work: what every evaluator in the Channel 0.2 property gate does with a record it cannot
read, at `a3b22f4`,
`Merge pull request #149 from Niizuki/channel-0.2-condition-4-fifteenth-pass`

Date: 2026-09-06

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **sixteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not clean.** Its frozen set reported nothing. The instrument it built found **BB1**, **BB2**
and **BB4** in the package on its first run, and **BB3** is a defect in a frozen instrument that the
new work collided with — so the ruling's second test is not met and the two-consecutive count stays at
zero. It raises **BB5**, **BB6** and **BB7** against its own new code; under the ruling those belong
to neither counted population, and they are numbered because the corrections cite them.

**BB7 is the one to read first.** The instrument this pass built has a silent-blindness mode: invoked
the way the coverage measure invokes a gate, it recorded no reads at all and reported a clean *0 raw
reads* over a package it had entirely stopped reading. The coverage measure — a frozen instrument —
reported eleven constructs of the census as never executed, **and the exemptions to silence that were
drafted before the question "why did these not run" was asked.** The package findings below stand
because they were found under the invocation where the census does work; that they were is a fact this
pass had to establish rather than assume.

The method was not chosen by this pass. The fifteenth's own finding set it. AZ1 made the `Errors`
channel reach its consumers and BA1 stopped three composed evaluators from rebuilding it empty, and
both left the question one level in untouched: **nothing required an evaluator to FILL that channel.**
One evaluator of twenty-six populated it at all, so for the other twenty-five *I could not read this
record* and *this realization conforms* were the same verdict, and no instrument here could tell them
apart.

## Section numbering

**BB1**, **BB2** and **BB4** are what the instrument this pass built reported about the package on its
first run. **BB3** is against a frozen instrument, found in the way BA7 was — by new work running into
a gap the instrument had always had. **BB5**, **BB6** and **BB7** are defects in this pass's own new
code.

The tally in the plan counts all seven, because that is what a reader counts. Where the ruling's two
populations differ from the total, the split is stated rather than the total adjusted.

## The frozen set, run first

**Zero.** The design gate, the owned-fact gate, the return-channel census, the coverage measure, the
whole 97-probe corpus, the declared property corpus, the 2,000-vector generated run and AZ3's
dropped-field sweep were all green before any of this pass's work existed. Tenth consecutive clean
frozen set, and strictly larger than the fifteenth's: it gained the return-channel census and the
twelve probes that pass added.

It is reported with the caveat every pass since the fourteenth has had to make, and this one makes it
about the sharpest instrument in the set. **The frozen set could not have reported the class below.**
AZ3's sweep is the one instrument here that removes a published field and asks what the property does
— and it removes eighteen fields of one record shape, for one property, over generated vectors. Every
other instrument asks whether a check runs, whether a guard fires on its own subject, or whether a
property is red where it should be green. An evaluator that reads a field it cannot read runs
perfectly, fires nothing, and is green.

### BB1 — twenty-five of twenty-six evaluators read their records raw

`Invoke-C4P2` reads every operand through `Get-Field` and pairs each with an explicit entry in its own
`Errors` list where the operand is required. It is the one evaluator that does. The other
twenty-five read the vector directly — `$sessionEvent.step`, `$sessionEvent.accepted`,
`$interaction.identity`, `$session.establishedBound` — and a direct read of an absent field yields
`$null` silently.

What that costs is not uniform, and both halves are the same defect:

- **Silently green.** `Invoke-S1` opens `if ([string]$sessionEvent.step -ne 'transition' ... ) { continue }`.
  A timeline event that does not publish `step` is not a transition, is skipped, and the property
  returns green having evaluated nothing about it. The same shape governs `S2`, `S3`, `S4`, `I1`,
  `I3`, `I5`, `C2-P1`, `C3-P1`, `C5-P1`, `C6-P1`, `C7-P1` and `C9-P1`: the selector that decides
  whether a record is in scope is the field whose absence removes it from scope.
- **A violation the property cannot substantiate.** `Invoke-I5` writes
  `$bounds[[string]$session.id] = [int]$session.establishedBound`, and `[int]$null` is **0**, so a
  session that does not publish its bound is evaluated against a bound of zero and the first admission
  breaches it. `Invoke-S1` builds `"$($sessionEvent.from)>$($sessionEvent.to)"`, so an absent `from`
  makes the edge `>established`, which the legal table does not contain. That is AU2's other half —
  an obligation that fires on what the vector did not SAY — one level below where AU2 found it.

**Measured rather than argued.** The census below poisons each field of each vector in turn and asks
which reader performed the read. Against the package as this pass found it, **933 reads of a field the
evaluator could not read**: 48 went through `Read-Required` and reported, **760 left the property
green having reported nothing**, and **125 moved the verdict with nothing reported**. After the
correction the count is **945**, all through a named reader — twelve more rather than fewer, because
routing a read through `Read-Required` takes it out of a short-circuit that used to skip it, which is
the point: a field the property depends on is now read whether or not today's input makes the
comparison reach it.

**The correction is a taxonomy, not eighty patches.** Every read now goes through one of four readers,
and the four differ in what an absent field MEANS — which is the thing the raw reads had no way to
say:

| reader | absent means |
| --- | --- |
| `Get-List`, and `Get-Timeline`/`Get-Interactions`/`Get-Sessions` over it | the collection is empty — AU2's ruling, unchanged |
| `Read-Required` | the record cannot be evaluated; reported against the vector |
| `Read-Optional`, with a mandatory reason | absence is a fact the design states, and the call site says which |
| `Get-Field` | absence WIDENS the candidate set — closure review 16's P3 rule, `C4-P2` only |

`Get-List` gained the record and the field name rather than taking a value, because
`Get-List $interaction.terminalHistories` performed the read in the CALLER: the function only ever saw
what had already been read, which is a producer's channel rebuilt one level out.

### BB2 — `C2-P1`'s middle clause evaluated one admit event of fifty-one

Routing `Invoke-C2P1`'s reads reported that no vector but one publishes `acceptedTransition`, and the
clause is gated on it:

```
if ([string]$sessionEvent.step -ne 'admit' -or -not $sessionEvent.acceptedTransition) { continue }
```

`acceptedTransition` occurs **twice in the repository**: on that line, and on one admit event of
`C2-accept-interaction-while-draining`, which is the property's own named mutation. No design artifact
names it. So the clause the contract states as *every other input leaves the prior state unchanged or
enters `faulted`* evaluated **one of the fifty-one admit events in the declared corpus and none at all
in the generated population**, and was green everywhere else by never having looked.

That an `admit` step happened is what the timeline records by carrying the step; the second field was
a fact already stated by the step's own presence, which is W1's duplication arriving as a guard. The
guard is deleted and the field with it. The clause now evaluates every admit event in the corpus, is
still green over all 55 declared inputs and 100 generated vectors, and is still red on its mutation.

**This is the finding that says what BB1 was worth.** BB1 is a property of the code; BB2 is what that
property was hiding, and it was not found by reading the clause — three passes have read this file —
but by a mechanical question that had never been asked of it.

### BB3 — the return-channel census cannot see an accumulator written by index

The census BA built declares that every `$script:` collection anything adds to must be declared
`per-evaluation` or `cumulative`. It discovers them by walking the syntax tree for an
`InvokeMemberExpressionAst` whose member is `Add`.

So `$script:Table[$key] = $value` is invisible to it. That is the same channel written the other way:
undeclared, and therefore unchecked for a consumer, with the gate green.

It was found by writing one. This pass's `$script:OptionalReads` records which declared-optional field
some input left absent, it is written by index, and the census reported instead that the declaration
for it applied to nothing. **That is BA6's class inside the instrument BA6 was raised in** — the
argument against recognising a thing by the syntax someone happened to write, made a second time
against the same file, one commit later.

Corrected by also walking assignment statements whose target is an index expression over a `$script:`
variable. The limit that remains is stated in the file rather than closed: a write through an alias,
or through a member other than `Add` on a collection type that has one, is still invisible.

### BB4 — a whole-record comparison reports a null leaf as a difference

`S5` asks whether fixed and negotiated establishment produce the same normative profile record. The
operand is the RECORD — naming its fields in the gate would make this file a second surface for the
profile's shape — so the comparison renders both subtrees and compares the renderings.

A leaf the vector does not publish renders as `null`. The two renderings then differ, and `S5` returns
*produces different normative profile records from fixed and negotiated establishment*. It is AU2's
half that fires on silence, reached through a comparison rather than through a read, which is why
every previous instrument here missed it: no field of `S5`'s is unread, and no condition of `S5`'s
fails to run.

Corrected by performing the rendering inside a reader. `Read-Rendering` reads the named field through
`Read-Required`, renders it, and reports a `null` in the rendering as an unreadable record rather than
letting it become a verdict — a normative profile record carries no nulls, and one in the rendering is
a leaf the vector left out. The corpus carries two distinct profile records and no null leaf, so the
check is green today and the probe `BB4-a` is what shows it is not green by never having looked.

### BB7 — the census reported a clean zero over a package it had stopped reading

The poison getter is a scriptblock made with `.GetNewClosure()`, and it called a function this script
defines to walk the call stack. `GetNewClosure` snapshots the **variables** of the enclosing scope into
a new module scope; a function the script defines is not resolvable from that scope when the script is
invoked through the **call operator** rather than through `-File`. The getter then throws
`CommandNotFound` — and a `PSScriptProperty` getter that throws yields `$null` to its reader **without
surfacing the error**.

So every read came back unattributed, `$censusSiteReaders` stayed empty, and the census printed

```
Channel 0.2 read-provenance census: 0 of 942 poisoned fields were read by an evaluator,
0 of those through a sanctioned reader and 0 raw, over 5 exercised Read-Optional declarations.
```

and **passed**. Zero raw reads is what a clean census says. Zero raw reads is also what a census that
has stopped reading says, and nothing distinguished them — which is the exact defect class this whole
pass was built to find, in the instrument built to find it, in the same commit. It is **BA6** one pass
later and total rather than partial.

**How it was found, and the part worth keeping.** The coverage measure runs each gate in a child
process through the call operator. It reported **eleven constructs of the census as never executed** —
and those eleven were every construct downstream of a read being recorded, which is what that shape of
report means. The exemptions to silence all eleven were drafted first. They were not written, because
the eleven had one thing in common that an exemption would have asserted away: *a construct that never
runs is a finding until it is explained*. Reading them as a symptom rather than as noise is what
produced this finding, and it is the practice this section exists to record.

Corrected in both halves:

- **the mechanism** — the call-stack walk is written out inside the getter, so it calls no script
  function. `Get-PSCallStack` is a cmdlet and resolves from the closure's scope; a script function does
  not. The census now reports the same `945 of 3,594` under `-File` and under the call operator, which
  was verified both ways; and
- **the silence** — a census that poisons every field of an input and observes the property reading
  **none** of them is now a failure. Every property reads at least one field of every input it is
  declared green on, so zero reads is the machinery failing to observe, never the property failing to
  read. It is asserted **per property and per input** rather than as one total, because a census
  blinded for one property and working for the rest is the partial case BA6 was, and a total would hide
  it.

The second half is the one that matters. The first fixes today's defect; the second is what fails the
next one without another instrument having to notice, and its probe is `BB7-a`.

### BB5 — nine of the fourteen `Read-Optional` declarations this pass wrote were unfalsified

`Read-Optional` is a judgement: it says an absent field is a fact the design states rather than a
silence in the vector. A judgement nothing checks is a way of spelling a raw read, so the reader
records each declaration and the gate fails on one **no declared input exercises** — if every record
publishes the field, the claim that its absence is meaningful is unfalsified and the read is a
`Read-Required` written the long way. That is AU1's unit one level out.

Written for the first time, fourteen declarations went in and the check reported **nine**. All nine
were wrong in the same direction: `observations`, `establishedProfileRecord`, `explicitEvidence` at
three sites, `observationComplete`, `authorityRecord`, its `effectCertainty`,
`deterministicExpectedObservation`, `possiblePostDispatchPath` and `provenanceForm` are published by
every record that reaches the read. Each is now `Read-Required`, which is the stronger reader, and the
five that survive are the ones some input genuinely leaves absent.

The check earned its place immediately: two thirds of the first draft's judgements were guesses, and
without it they would have read as declarations.

### BB6 — the new collection reader's two unrolling defects

Both are PowerShell's collection unrolling and both were found rather than reasoned about.

`Get-List` returning `@()` hands the caller **`$null`**, because a function's returned collection is
unrolled. `$admitted = Get-List $observations 'recipientAdmittedIdentities'` therefore bound `$null`
to a parameter that does not allow it, and the gate threw on the first run after the conversion. That
is **BA5**'s defect in this pass's own new code — the same unrolling, one function along.

Corrected with the unary comma, `return ,@(...)`, which is the idiom for it. **And the correction has
its own half**: the comma survives exactly ONE unrolling, which is what makes it right for an
assignment and wrong in a pipeline. `Test-MemberOf` flattens the admitted sets with
`$sets | ForEach-Object { Get-List $_ 'identities' }`, and there the wrapper is consumed by the
pipeline and the inner array arrives whole, so `[string]$_` rendered every identity into one
space-joined string that matches nothing.

**AZ3's sweep caught it**: two droppings it declares red went green. Nothing in the declared corpus
moved, and the property's own named mutations stayed red. It is the clearest thing in this pass about
why a measure whose inputs are not the ones the change was written against is worth its cost.
Corrected with an explicit loop, and the reason is written where the comma is.

## The instrument, and what it reports

`build/verify-channel-0.2-properties.ps1`, as a new unit beside AU1's obligation check, because it has
to RUN the evaluators and they live there.

**How it measures.** Each field of each vector is replaced, one at a time, by a property whose getter
records that it was read and returns nothing: the record still has the field and the field has no
value. The evaluator is then run. A getter that never fired says the evaluator does not read that
field. A getter that fired says the evaluator read a field it could not read, and the census asks one
question about it — **which reader performed the read**, taken off the call stack rather than inferred
from the outcome.

**Why the reader and not the outcome.** The outcome is the demonstration; the reader is the rule.
Classifying by outcome would pass a raw read whose absence happens not to move today's verdict, which
is the same mistake as measuring a guard by whether today's corpus makes it fire — AP1's class, and
the reason the coverage measure counts conditions rather than failures. So the census fails on a raw
read and reports what that raw read DID as the evidence: `left the property green and reported
nothing`, or `moved the verdict to red`.

It reports **945 of 3,594 poisoned fields read, all through a sanctioned reader, over 5 exercised
`Read-Optional` declarations**. Six probes were added, taking the corpus from 97 to 103, one of them a
`pass` probe over the census's own first declared limit and one, `BB7-a`, over the blindness BB7 was.

**The walk prunes, and the pruning is what makes it affordable.** A field beneath a container the
evaluator did not read cannot itself have been read, because the container is the only path to it —
so a container whose poisoning goes unread takes its whole subtree out of the walk. Flat, the walk
poisoned **10,862** fields to find those 945 reads; pruned it poisons **3,594** and finds the same
945, and the properties gate at `-GeneratedCount 0` goes from **10.6 seconds to 3.2**. The two forms
were run against each other — including with a raw read injected, where both report the same 40 raw
reads — before the flat one was removed.

**Three limits, stated in the file where they apply rather than discovered later:**

- it measures the reads a DECLARED green-expected input provokes. A read on a path no such input
  reaches is invisible, exactly as an unreached guard is invisible to the probe corpus. `BB1-c` is the
  probe that demonstrates it rather than asserting it;
- `declaredSteps` is outside the poisoned set, because the harness builds the step index from it once
  before any evaluator runs. AZ3's sweep is what covers that surface; and
- a `Read-Optional` is a judgement that the census checks was *declared and exercised*, not that it
  was *right*. Nothing here can check that, and the reasons are written at the call sites so a reader
  can audit them against the artifacts.

## Findings

| id | where | what |
| --- | --- | --- |
| **BB1** | package | twenty-five of twenty-six evaluators read their records raw, so an unreadable record and a conforming one produce the same verdict |
| **BB2** | package | `C2-P1`'s middle clause was gated on a field published by one admit event of fifty-one and named in no artifact |
| **BB3** | package, frozen instrument | the return-channel census recognises an accumulator by `.Add(...)` and is blind to index assignment |
| **BB4** | package | `S5` renders two profile records and compares them, so an unpublished leaf reports as a difference |
| **BB5** | this pass's own | nine of fourteen new `Read-Optional` declarations were unfalsified by any declared input |
| **BB6** | this pass's own | the new collection reader's two unrolling defects, the second caught by AZ3's sweep |
| **BB7** | this pass's own | the census recorded no reads at all under the invocation the coverage measure uses, and reported that as a clean zero |

## What this pass verified rather than believed

- **Both units of the new instrument were made to fail for their claimed reason before being
  believed.** One raw selector read put back into `Invoke-I1` takes the census from 0 raw reads to 40
  and names the property, field, record, source line and observed effect. One `Read-Optional` pointed
  at a field every record publishes fails the exercise check with the declaration quoted back.
- **BB2's correction was verified in both directions**: the clause is green over all 55 declared
  inputs and 100 generated vectors with the guard gone, and still red on its own named mutation.
- **BB6 and BB7 were each found by a frozen instrument and not by inspection** — AZ3's sweep and the
  coverage measure — which is the whole argument for keeping both in the gate.
- **BB7's guard was pinned with the defect itself**: the script-function call put back into the getter
  and the gate run through the call operator reports the census observing no read for every property
  and input, where before it reported a clean zero.
- **The normal path is unchanged**: 26 of 26 properties, 131 evaluations over 55 declared inputs, nine
  operand mutations, 2,600 generated evaluations at 0 red, eighteen droppings with eight load-bearing.

## What remains outside the pass

**The census measures the reads, not the judgements.** Its five surviving `Read-Optional` declarations
say that an absent `refusal`, `decisionPoint`, `initiatorAttribution`, `provenanceFormActually` or
`terminalHistoryChangedBy` is a fact the design states. Each is checked to be exercised by some input
and each is written beside the read; **none is checked against the artifact that would settle it.** A
declaration that is exercised and wrong looks exactly like one that is exercised and right, and that
is the sharpest question a seventeenth pass could ask of this pass's own work.

**The generated population still carries one frame shape per session**, and the generator still
produces conforming vectors only, with the mutation direction applied by hand and discarded. The
fourteenth left this and the fifteenth did not reach it; neither did this one.

**The census does not reach the other five dispatches.** It poisons fields for the declared corpus and
not for the generated loop, the operand harness's two, or AZ3's sweep. Whether an evaluator reads a
GENERATED record raw is the same question over a population a thousand times larger, and it costs a
thousand times more; the trade was not made here and is left named.

**Cost.** The census adds to a gate that the probe corpus re-runs once per probe, so its price is paid
once per probe on the scheduled run rather than once in total. Pruned it is about **1.5 seconds** of the gate's 3.2
at `-GeneratedCount 0`, where flat it was 7.4 of 10.6, and that is what puts it in the per-commit gate
rather than behind the self-check switch. What the scheduled run actually costs with it is the figure
the plan's section 4 owns, and it is measured there rather than predicted here.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, seven, **seven** — four of this
pass's seven in the package and three in its own new code.

## Where this family is dispositioned

**BB1** through **BB7** are corrections to the verification instruments, their declarations and one
input field, not to the design, so under the 2026-08-20 ruling they belong in the verification
foundation plan's own record and not in the completeness review's disposition index. The plan's
section 2s carries them, and this document is the pass's evidence.
