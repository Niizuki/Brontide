# Channel 0.2 eighteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-eighteenth-pass-2026-09-11-3fa4de6`

Reviewed work: whether the artifact a reader's declaration cites says what the declaration claims, at
`3fa4de6`, `Merge pull request #151 from Niizuki/channel-0.2-condition-4-seventeenth-pass`

Date: 2026-09-11

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **eighteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not clean.** Its frozen set reported nothing, for the twelfth consecutive pass. The instrument
it built found **BD1** and **BD2** in the package on its first run, so the ruling's second test is not
met and the two-consecutive count stays at zero. Both are in the verification and neither in the
design. It raises **BD3** against its own new code; under the ruling that belongs to neither counted
population, and it is numbered because the correction cites it.

The method was not chosen by this pass. The seventeenth named it, and named it as the half its own
instrument could not reach: that pass checked the **polarity** of the inputs exercising a declaration
— an optional needs a green-declared input to leave the field absent, an obligation a red-declared one
— and wrote in the file that polarity is a proxy. A reason citing a contract sentence that does not say
what the reason claims is exercised, correctly polarised, and wrong. Five declarations survived it,
and the seventeenth's own closing table said that two of them were named by no design artifact at all.

**This pass made every declaration cite, and the gate read the citations back.** Three resolved. Two
could cite nothing but the verification foundation plan — the one document that states their rule —
and the instrument reported both, in the words it now uses for that case: *a rule the design does not
state is the gate's own convention*.

## Section numbering

**BD1** and **BD2** are what the instrument this pass built reported about the package on its first
run. **BD3** is a defect in this pass's own new code, found by a frozen instrument in the way BB7 and
BC3 were found — and by the same instrument that found BB7.

## The frozen set, run first

**Zero.** The design gate, the owned-fact gate, the return-channel census, the text and link guards,
the whole 105-probe corpus, the coverage measure over four gates, the declared property corpus, the
2,000-vector generated run, AZ3's dropped-field sweep over 500 of them, the read-provenance census and
the declaration-polarity check were all green before any of this pass's work existed. Twelfth
consecutive clean frozen set, and strictly larger than the seventeenth's: it gained the polarity check
and that pass's two probes.

It is reported with the caveat every pass since the fourteenth has had to make. **The frozen set could
not have reported the class below, and the seventeenth pass said so.** Every instrument in it asks
whether a check runs, whether a guard fires on its own subject, whether a property is red where it
should be green, which reader performed a read, or which polarity the inputs exercising a declaration
carry. None of them reads what the design says. A declaration whose reason is a rule the design never
stated is declared, exercised, correctly polarised, and green.

## The instrument

A meaning-carrying declaration is now made as one thing rather than two. `Read-Optional` and
`Read-Obligation` take a declaration carrying `Because`, `Artifact` and `Words`, and the reader
records the citation under the same key the polarity tables use, so the check at the end of
`build/verify-channel-0.2-properties.ps1` reads a declaration's polarities and its citation off one
key. The check is two conditions:

- the cited artifact is one of the design's. Which artifacts those are is **derived from the
  declaration file the gate already executes** rather than listed a second time: the artifacts
  `channel-0.2-properties.json` names as stating a property, and the ones its authority block names
  — except the verification foundation plan, which that block lists as the authority for the gate's
  own history and which the Channel index records as not a design artifact and assessed by no closure
  review. A declaration citing the plan cites the gate's own convention written down one document
  over; and
- the artifact contains the cited words, flowed and emphasis-stripped as the property statements are
  compared, and otherwise verbatim and case-sensitive. A citation that has to be paraphrased to be
  found is one whose sentence moved.

Both guards were made to fail for their claimed reason before either was believed, and both are
pinned in the corpus as `BD-a` and `BD-b`. The first-run report over the package as found, with the
two uncitable declarations pointed at the plan, is what the two findings below quote.

### BD1 — `I7` read a fact the timeline already states, through a field only its mutation carried

`I7` is *a terminal fact for one interaction changes no sibling interaction's terminal history*. The
evaluator read `terminalHistoryChangedBy` off each interaction record, declared optional with the
reason *an interaction whose terminal history no sibling changed does not record a changer*. The
instrument reported:

> cites `Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md` for the words "an absent changer
> means no sibling changed it", and that is not a design artifact

That is the reader saying the rule is nowhere in the design, and it is right. What is in the corpus
is the shape BB2 found one property over: the field occurs on **one interaction record in the whole
corpus, the mutation's**. Every other record leaves it absent and the absence was read as a fact.

What the timeline already states is which interactions a terminal fact changes. Every accepted
`terminal` step names the admitted interaction it `closes`, `C4-P1`'s first clause reads that field
to require that it closes exactly one, and the mutation `C4-terminal-closes-two-interactions` is
written through it. So the fact was published twice — once by the step that carries it and once by a
field beside it that only the mutation set — and the second copy carried a convention of its own,
which is W1's duplication arriving as a judgement. **The correction reads the timeline**: a terminal
fact for one interaction closes that interaction, and one that closes a sibling has changed the
sibling's terminal history. The field is deleted, and `I7-sibling-terminal-history-changed` states its
mutation where the timeline says what a terminal fact changes: the accepted terminal for `i1` closes
`i2`.

**Pinned before it was believed.** Restoring `closes` to `i1` on that step takes the gate red three
ways at once — `I7` green on its named mutation, `I7` green on every declared input, and the new
obligation named as one no declared input makes fire — which is the same evaluator being reported by
three frozen instruments as unfalsifiable, and it is what the corrected vector reaching the clause
looks like from the other side.

### BD2 — `C9-P1` read the vector's expected provenance as optional, and the brief says every vector carries it

`C9-P1`'s second clause is *no field permits a local inference to be accepted as a peer statement or a
protocol fault as an Outcome*. The evaluator compares `provenanceForm`, what the realization recorded,
against `provenanceFormActually`, what the vector says the observation was, and read the second as
optional with the reason *the vector states what an observation actually was only where that differs
from the recorded form, so an absent field is agreement and not silence*. The instrument reported the
same sentence over the plan's words *an absent actual form means agreement*.

Here the design does not merely fail to state the rule; the artifact that owns the vector format
states the opposite. The neutral brief's vector format has every vector carry *expected frame decision
and peer/local provenance*, and says *expected observations are complete data, not prose interpreted
by adapters*. The corpus was stating the expected provenance on exactly one interaction record — the
record of the mutation that disagrees with it — which made an absence carry a verdict the brief says
the vector must state. **The correction is `Read-Required`**, as the brief says the vector carries it:
every interaction record that records a provenance form now states the expected one as well, twenty-
nine records across the corpus, and the generator emits it on every interaction it builds. On
`C9-provenance-form-outside-the-four`, which records `peer-inference` for what its timeline states
was an Outcome, the expected form is `semantic-outcome`.

**Pinned before it was believed.** Removing the field from `S-conforming-single-session`'s one
interaction has `C9-P1` report that record as one it could not read, through `Read-Required`'s own
channel: *an obligation cannot tell a realization that violates it from an input that does not state
the fact*. The first attempt pinned it on `C9-provenance-form-outside-the-four` and the gate stayed
green, because that mutation returns red through the closed-set clause before the expected form is
read — recorded here because a pin that is green for that reason is AN1's class, a check whose reach
is narrower than the claim, and the second pin is what showed where the read is reached.

### BD3 — the new check's first loop was written where the coverage measure cannot see it

The check collects the citable artifacts in two loops, and the first was written on one line:

```
foreach ($statingProperty in $properties.properties) { [void]$citableArtifacts.Add(...) }
```

The coverage measure — a frozen instrument — failed on it, on its first run over this pass's own code:

> this foreach has its body on the header line, so whether the body ever ran cannot be decided by a
> line trace -- the header runs either way. It is not known to be uncovered; it is unmeasurable, and a
> construct this measure cannot decide must not read as one it has passed.

That is **BA7**'s class, in the commit after the pass that wrote BA7's rule into the measure had
become part of the frozen set. The measure decides whether a `foreach` body ran by whether the body's
first line ran, and a body on the header line reports as covered either way; BA7 split sixteen such
loops and exposed three never-run bodies among them. The measure now refuses the shape outright rather
than passing it, which is why this was reported and not silently counted — and it says in its own
message not to exempt it, because an exemption asserts the construct is correctly unreachable, which is
exactly the fact in question. Corrected by putting the body on its own line. The loop runs on every
input, so nothing about the check's verdict changes; what changes is that the measure can now say so.

## What the three surviving citations resolve to

| declaration | cites | the words |
| --- | --- | --- |
| `Read-Optional 'refusal'` | the capability contract, C8 | *An interaction reaches exactly one terminal history: local refusal before dispatch, semantic Outcome, peer protocol fault, locally observed loss, or cancellation completed by a valid terminal Outcome.* |
| `Read-Obligation 'decisionPoint'` | the capability contract, `C6-P1` | *every denial or unevaluatable presentation records the decision point, initiator attribution, and `known-none`* |
| `Read-Obligation 'initiatorAttribution'` | the same sentence | |

The first is the citation the reason always implied and never named: a refusal before dispatch is one
of five terminal histories, an interaction reaches exactly one, so one whose terminal history is any of
the other four has no refusal to record. The gate now reports **1 `Read-Optional` and 2
`Read-Obligation` declarations, each checked against the declared verdict of the inputs whose silence
exercises it and against the words of the design artifact it cites**, where the package as found
reported three of the former.

## Findings

| id | where | what |
| --- | --- | --- |
| **BD1** | package | `I7` read `terminalHistoryChangedBy` as a fact the design states; no artifact states it, the field occurs on one record in the corpus, and the timeline already states what a terminal fact changes through `closes` |
| **BD2** | package | `C9-P1` read `provenanceFormActually` as stated only where it differs from the recorded form; no artifact states that, and the brief that owns the vector format has every vector carry its expected provenance as complete data |
| **BD3** | this pass's own | the citation check's first loop had its body on its header line, where the coverage measure cannot decide whether it ran — BA7's class, reported by BA7's own instrument |

## What this pass verified rather than believed

- **The frozen set was run at `3fa4de6` before any of this work existed**, the probe corpus and the
  coverage measure in a clone rather than the working tree, and everything reported above as green
  was read from that run's output rather than assumed from the previous pass's.
- **Both new guards were made to fail for their claimed reason before the correction was believed.**
  A citation whose `known-none` becomes `known-nothing` is reported as words the artifact does not
  contain; a citation of the plan is reported as not a design artifact. Each is pinned by a probe,
  `BD-a` and `BD-b`, taking the corpus from 105 to 107, and each was run and returned the verdict its
  guard owes.
- **Both corrections were pinned by deliberate failure**, as the two findings above record, and the
  second pin's first attempt was green for a reason worth keeping.
- **The coverage measure was run over the corrected package, in a clone at the branch head, and BD3 is
  what it reported.** Every other conditional and operand the pass added is evaluated by the passing
  run, and none needed an exemption.
- **The return-channel census saw the new accumulator.** `$script:DeclarationCitations` is declared
  cumulative beside the two polarity tables, and the census — the frozen instrument that caught BC3
  — is green over it, which means the declaration is anchored on a write it can see.
- **The corpus's own count moved with the two probes**, and the plan's section 4 is corrected from
  105 to 107 in the same change rather than by the corpus failing on the figure a commit later. AU4's
  machinery is what would have caught it; this pass did not wait for it to.

**One correction was made on contact rather than found by the instrument.** Five interaction records
recorded the provenance form `local-refusal`, which is outside the closed set of four the gate reads
and the contract's C9 enumerates. None was read by any property: `provenanceForm` is read only by
`C9-P1`, and the five vectors are in `C5-P1`, `C6-P1` and `I4`'s groups. They now record
`local-pre-dispatch-refusal`, the value the set holds for a refusal before dispatch. It is not
numbered, because it was found by reading and the ruling's populations do not count that — AW2's
question, still open — and it is recorded here because it is the brief the next pass inherits.

## What remains outside the pass

**The citation check reads presence, not meaning.** It asks whether the cited words are in the cited
artifact; it cannot ask whether those words settle what the reason claims. A declaration citing a
true sentence for a rule that sentence does not state is green here, and the reason and the words are
written beside each other at the declaration so that a reader can compare them. That is the limit
this instrument was always going to have, and the two findings above are what it can reach: a rule
cited nowhere, and a rule the cited artifact contradicts.

**A `Read-Required` read carries no citation.** It claims that the design says the vector carries the
field, and that claim is checked by nothing: `provenanceFormActually` is required now on the strength
of the brief's vector format, and the sentence that says so is in a comment. Which fields of a vector
the brief's format requires, against which fields the corpus's records actually state, is the same
question as this pass's put to every field rather than to the five that carried a reason — and it is
AX2's unit from the other side, since AX2 asked which declared field no property reads.

**Closed vocabularies are checked only where a property reads them.** The `local-refusal` values
survived because the property that reads the field never sees the vectors that carried them. The gate
declares its closed sets — the four provenance forms, the frame kinds, the latch values, the terminal
forms `C8-P1` treats as non-semantic — and checks each at the one evaluator that reads it. A value
outside a closed set on a record no property reads is AX2's shape with a wrong value in it, and the
next unit is the obvious one: every closed-set field of every record in the corpus against its set,
regardless of which property's group the vector is in.

**The corpus writes `refusal: null` and the reader cannot tell it from an absent field.** The
interaction records state the absence of a refusal positively, and `Read-Optional` reads a member
whose value is null as absent. That is a fact for the owner question the seventeenth pass put — should
a vector state a realization's omission positively — rather than a finding: the corpus already does,
in form, for one field, and nothing reads the difference.

**The generated population still carries one frame shape per session**, and the generator still
produces conforming vectors only. Unchanged since the fourteenth, and unreached by this pass.

**Cost.** The citation check reads two artifacts once each and is unmeasurable beside the census. The
corpus gains two probes at the properties gate's `-GeneratedCount 0` cost each.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, seven, seven, three, **three** —
two of this pass's three in the package and one in its own new code.

## Where this family is dispositioned

**BD1** through **BD3** are corrections to the verification instruments, two evaluators, one declared
input and the corpus — not to the design — so under the 2026-08-20 ruling they belong in the
verification foundation plan's own record and not in the completeness review's disposition index. The
plan's section 2u carries them, and this document is the pass's evidence.
