# Channel 0.2 ninth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-ninth-pass-2026-09-03-296dd1e`

Reviewed work: the verification-foundation work done under the closure-cycle hold -- W1 (the owned
facts and their gates), W2 (the twenty-six executable properties), W3 (the status blocks and Channel
index rows), the retained guard corpus, the coverage instrument, and the AT corrections -- at
`296dd1e`, `Merge pull request #141 from Niizuki/ci-raise-repository-gate-ceiling`; raised and
corrected AU1-AU5

Date: 2026-09-03

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **ninth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it**, because it found five defects and corrected them.

## Method

The pass began from `origin/main`, fetched immediately before the work. As the AT review asks of it, it
started by running the retained instruments rather than rebuilding them: **73 of 73** probes green in
one command, and the coverage gate green. Neither had rotted.

It then took the brief the AT review left, and the first thing it had to decide was whether that brief
was answerable as stated.

**The inherited unit does not separate.** AT left the class "an operand that is evaluated, always takes
the same value, and could be deleted without changing any observed verdict" -- 124 of 247 -- and named
the open problem as separating a defensive null check from a second semantic obligation hiding beside
it. Pursued directly that stays unanswerable: a null check and a semantic obligation are the same shape
in the syntax tree, and any rule that tells them apart is a rule about what the code *means*.

**So the pass changed the unit rather than the analysis.** The property gate routes every verdict
through one constructor, `New-Red`. A measure over its call sites therefore contains only semantic
obligations -- there are no null checks in it, because a null check does not state a verdict -- and
needs no separation rule at all. That is the same move AR2 made from statements to conditions and AT
made from conditions to operands, made once more against the grain of the inherited brief.

It reports **eleven**, and the reduction from 124 is not a narrowing of the question. Every one of the
eleven is a distinct obligation of a named property, and the pass confirmed each by deleting it: with
the obligation removed the property gate, the design gate and the coverage gate were all green.

The measure is retained rather than used and discarded, which is section 1.1 of the plan applied for the
fifth time. It is structural: an obligation is a `New-Red` whatever the contract calls its clauses, so
the class is total over the file by construction and a twelfth obligation joins it without registration.
That is **AL1's** correction pattern -- structural rather than lexical -- applied to the class AR1 and
AT1 each closed lexically and each left open one level down.

## Findings

### AU1 -- eleven property obligations that no declared input reaches -- corrected

Eleven of the forty-two obligations in `build/verify-channel-0.2-properties.ps1` are evaluated on every
declared input and never once fire. Each was deletable outright with every gate green, so nothing in the
suite distinguished an implementation that honours the obligation from one that does not.

| Property | Obligation with no input that reaches it |
| --- | --- |
| `C2-P1` | the first clause, S1's legal transition table |
| `C2-P1` | the third clause, S4's terminal monotonicity |
| `C3-P1` | class and direction matching the session's established profile |
| `C5-P1` | every positional Shape rule, the second obligation of `C5-P1-clause-1` |
| `C7-P1` | a dispatched relational interaction matching exactly one lifecycle declaration |
| `C7-P1` | the pre-Ready window |
| `C8-P1` | the first clause, I2's one-terminal-history rule |
| `C9-P1` | the provenance form being one of the four |
| `C10-P1` | the observation being complete for its provenance form |
| `C11-P1` | the established profile supporting every required facet |
| `I6` | a relational interaction matching exactly one declaration |

**This is AR1 and AT1 a third time, and `C5-P1` is the sharpest instance.** AR1 was raised because
`C5-P1` and `C6-P1` each had one named mutation firing through the first of two clauses, and it closed
the class "by the gate rather than the two instances by hand" -- with a check over properties that
*declare* a conjunct. AT1 found `I4`, which declares none. Here, `C5-P1-clause-1` -- the clause AR1
gave a mutation -- states **two** obligations: the mutation fails a declared bound and returns, and the
positional Shape obligation beside it was still deletable with every gate green. The rule was right
every time and stated at the wrong altitude every time: over declared conjuncts, then over unevaluated
operands, and neither reaches an obligation that is evaluated and never fires.

**`C2-P1`'s third clause was unreachable by construction, not by a missing input.** No legal edge leaves
`closed` or `faulted`, so every input that takes S4 red also takes S1 red, and C2-P1 reads S1 first.
`S4-terminal-session-resumed` therefore fires C2-P1's *first* clause. The correction is a vector that
closes the session and then records an accepted `established>draining` transition -- an edge the table
does contain -- so the monotonicity violation is witnessed with the legal table satisfied.

**Corrected.** Eight new vectors and two existing ones now pin all eleven, each declared in the
completeness review's audit rows, which are the authority the gate checks the declaration against. Two
obligations needed no new input: `S1-illegal-transition-accepted` and `I2-two-terminal-histories`
already produce the red, and C2-P1 and C8-P1 are evaluated *through* those machine properties rather
than restating them, so declaring the membership is the whole correction. That is the precedent I4's
row states for `C5-pre-dispatch-refusal-possible-effect`.

The check is in the property gate and it **failed first**, reporting exactly these eleven from the
syntax tree -- derived independently of the runtime instrument that first found them, and agreeing with
it.

### AU2 -- six properties could not tell a violation from a vector that omits the field -- corrected

Auditing the eleven new mutations for what would make them false pins found that each could have been
"satisfied" by silence rather than by the violation it names. Six properties -- `C3-P1`, `C5-P1`,
`C6-P1`, `C7-P1`, `C11-P1` and `I6` -- were red on a conforming timeline whose interactions and sessions
published no detail fields.

Two causes, and the first is a language fact worth stating plainly: **`@($null)` is a one-element array
in PowerShell**, not an empty one. A collection a vector does not publish therefore read as a collection
holding one null, and `C11-P1` was red with a blank where the facet name belongs *in its own witness*.
Two evaluators already carried a local `if ($null -eq $history) { continue }` for exactly this, which
is the defect having been met once and patched at one reader. The second cause is that an unpublished
scalar is falsy, so five more properties read silence as violation.

**This is AE1's shape, latent.** It is not live today, because every declared input publishes the fields
its properties read. It becomes live the moment a required-green member does not -- and the probe that
pins it does exactly that, removing `profileMatch` from the conforming single-session realization and
reproducing `C3-P1` red on its own required-green member, which is AE1 verbatim.

**Corrected.** Published collections are read through `Get-List`, which returns an empty array for an
absent one; required scalars are read through `Read-Required`, which raises the absence against the
vector instead of returning a verdict. The gate now names the unpublished field before it compares the
verdict, so a reader is not sent to hunt a conformance defect that is not there. The collection half is
pinned by an additional-green member -- a conforming session that requires no facets and publishes that
by omitting the field -- on AO1's precedent, so reverting the read takes the gate red rather than
leaving the defect for the next vector that omits something.

### AU3 -- the probe corpus size, stated in four places with three values -- corrected

The corpus held 73. The plan said **72** in two places, its own section 2k said the AT pass took it
**from 69 to 73**, and the review policy said **69**. The one surface that was correct is the plan's
section 4 measure, which the harness recomputes.

**That split is AM2 exactly, one cycle later and in the same document.** AM2 was two of the plan's five
section 4 measures stating numbers the repository does not produce, and its finding was that "the
measures the gates compute were right and both left to prose were wrong". The correction made those five
computed and did not ask the AN question of the fact it had just corrected -- where else is this stated.

**Corrected.** The restatements are removed where a gate owns the number, and the harness now sweeps the
plan, the review policy, the Channel index and the future-work index for any stated probe count and
fails when one disagrees with the corpus it runs.

### AU4 -- the review policy's exact-next-work paragraph was a cycle stale -- corrected

`## Exact next work` said "**An eighth pass** over the same scope is the live path" while listing the
eighth pass as retained four lines above it, said "**Seven** such passes have run" above a list of
eight, and gave the seventh pass's leavings as the brief the eighth had already consumed. This plan's
section 3 said "the **seven** are not the same pass repeated" beside its own tally of eight.

This is the AJ2 class -- entry-point staleness -- at its ninth consecutive occurrence, and it is worth
saying what did *not* catch it. The AA1 correction made the Channel index's review counts structural,
recomputed from the reviews directory, and that check has held since. The review policy's own narrative
of iteration passes was left lexical, and it is the document that tells the next agent what to do.

**Corrected**, and the paragraph now cites the plan for the counts rather than restating them.

### AU5 -- the disposition index carried one row's AT clause twice -- corrected

The disposition index's section for the redesign plan ended with the AT disposition sentence repeated
verbatim, immediately after itself. The other eight sections carry it once.

It is small, and it is recorded because of what could not see it. Five freshness checks ask whether a
section *names* the newest family; none asks how many times, so a duplicated append is invariant under
every one of them. That is the same blind spot as **AQ5** -- an assertion that something must be
present fails loudly and an assertion about how much of it there is does not exist -- and it is the
first instance found in the record rather than in a gate.

**Corrected.** No check is added for it: a duplicate-clause rule over these rows would be a guard
written to the shape of one accident, which is the mistake AR1 and AT1 already paid for twice. It is
reported so the next pass reads the appends rather than only the family ids.

## What this pass verified rather than believed

- **The retained probes still apply.** 73 of 73, in one command, before anything else was touched. The
  AP review's measurement holds: running the corpus costs a command against the hour each pass before
  AO3 spent rebuilding it.
- **Each of the eleven obligations is deletable.** Not inferred from the trace -- each was deleted in
  turn on a copy and the property gate ran green on all eleven, and the design and coverage gates ran
  green on the one that was also checked against them. The repository tree was never mutated for this.
- **The obligation check fails first.** It reported the eleven before any correction, from the syntax
  tree rather than from the runtime measure that found them, and the two agreed exactly.
- **The AU2 correction is load-bearing.** Removing one published field from the conforming vector
  reproduces `C3-P1` red on its own required-green member, and the new check names the cause.

## What remains outside the pass

**The obligation measure does not generalize to the other gates, and that is the tenth pass's brief.**
It works because the property gate routes every verdict through one constructor. The design, facts and
guard gates raise `$failures.Add` from hundreds of sites with no equivalent chokepoint, so a guard there
that runs and cannot fail is found only by the probe corpus -- one guard at a time, and only where
someone thought to write the probe. Whether those sites can be given a comparable unit is open, and the
honest answer may be that the corpus *is* the instrument there and its completeness is the thing to
measure.

The 124-of-247 operand class the AT review left is **not** closed. It is set aside, with the reason
stated: the unit does not separate, and the eleven obligations this pass found were the part of it that
mattered. A later pass that finds a defect in that class outside the property gate should reopen it.

The guard harness and the coverage gate remain measured once rather than on every commit, which AT7
settled and this pass did not revisit.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, **five**; a pass with findings cannot satisfy condition 4 even when all
findings are corrected.

## Where this family is dispositioned

AU is a `design` family. AU1's corrections reach the completeness review's per-capability audit rows,
and under the 2026-08-20 routing ruling a finding whose correction reached the design is a design
finding whatever else it touched -- so all four are dispositioned in that review's history, and the
review policy's provenance table declares the family on both axes.
