# Channel 0.2 twenty-second W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twenty-second-pass-2026-09-18-ad5a6b4`

Reviewed work: the value of every field a property reads -- each given a wrong value of its own kind
on every declared input of both polarities, against whether any verdict moved -- and the two
reconciliations and four declarations that answer what it found, at `ad5a6b4`, `Merge pull request
#156 from Niizuki/claude/aw2-and-owner-rulings`; raised and dispositioned the BH1-BH3 findings this
document records, and BH4 and BH5 against its own code

Date: 2026-09-18

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twenty-second** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not.** The frozen set reported nothing at `ad5a6b4` -- the second consecutive clean frozen set
-- and the instrument this pass built found **three things in the package** on its first run, all in
the verification and none in the design. Under the ruling's two populations that is the outcome that
resets the count, and the count stays at zero. **BH4** and **BH5** are the pass's own code, each caught
by the frozen coverage measure on a first run over the branch.

## Section numbering

**BH1**, **BH2** and **BH3** are what the instrument reported on its first run over the package as
found, before anything was corrected. **BH4** and **BH5** are against the instrument itself and were
reported by a frozen instrument, which is the population BD3, BF10 and BF12 were counted in. It reported eight fields; the three findings account for four
of them, and the other four are recorded below under *What was declared on contact*, because reading
each against the artifact that owns it found the field inert by the design's own words or by an owner
ruling, and a limit the design states is a declaration to write down rather than a defect to number. That judgement is this pass's and is
stated so a reader can disagree with it.

## The frozen set, run first

**One.** In a short-path clone at `ad5a6b4`, before any of this pass's work existed: the text and link
guards, the design gate, the owned-fact gate, the return-channel census, the properties gate at its
default count and at the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors at
0 red, AZ3's dropped-field sweep over 500 of them with all eight load-bearing droppings reporting
where declared, the read-provenance census over both polarities at 1,520 of 5,553 poisoned fields
read and 0 raw, the field-readership census at 0 read by nothing, the closed-vocabulary census over
both populations, the declaration-polarity and declaration-citation checks -- the coverage measure
over four gates with the 41 condition and 6 operand exemptions it declared before this pass, and
the 134-probe corpus at 134 of 134 over 22 workers. All exit 0. The streak of clean frozen sets
stands at two.

## The instrument

Every instrument here asks whether a field is present, read, or in a set. None asked whether the
value read is **used**. The field-readership census the twentieth pass built ends at the dereference,
and said so: a read is a dereference, not an operand, and whether a value can move a verdict was
asked by AZ3's dropped-field sweep over three frame references and nowhere else. The twenty-first
pass repeated the limit as the first unit it left.

The operand census asks it of every field. For each (property, input) pair of both polarities, each
leaf field that property was seen to read on that input -- the readership census now keeps its reads
per pair -- is given a wrong value of its own kind, in place and restored: a Boolean is negated; a
number is moved one up and one down; a value in a closed vocabulary becomes each other member of its
set; an identifier becomes each other value the same field carries in the vector, and one no record
carries; a list of scalars loses its first member and gains a foreign one. The evaluator runs, and
the trial is **decisive** if the verdict, the conjunct, the errors, the unpublished fields or a thrown
exception differ from the baseline the run observed over the unmutated input, which is required to be
the declared verdict. Fields beneath `declaredSteps` and `delivery` are read through the step index,
so for a pair whose evaluation was seen to read the index -- replayed over a recording copy of it,
not assumed from the parameter every evaluator takes -- those are mutated too and the index rebuilt.
A field with trials and no decisive trial is **inert**: every property that reads it reaches the same
verdict whatever it says, so a wrong value there is invisible to the gate in exactly the way an unread
field's is, which is AU1's shape at the operand and W1's class on the corpus one level below BF's.

**On its first run over the package as found it reported eight of eighty-four fields inert**, over
3,540 trials and 137 pairs, seven of them reading the index. Four of the eight are the subjects of
three findings. The other four are limits the design or an owner ruling states, and are declared with
the words that settle each; BH3's field is declared too, on the silence it names.

### BH1 -- the declared session events were read into a witness alone, and one conforming vector declared two that never happened

`S6` reads each declared session event's `creates` list and names the event and its session in the
witness when one creates a forbidden thing. It never compares either, and nothing else reads them, so
`sessionEvents[].event` and `sessionEvents[].session` were stated on 104 records of 46 vectors and
decided nothing: the census found 20 trials on the event and 2 on the session, half of them moving
only the witness text and none a verdict.

That is AX2's shape -- a second surface for a fact the timeline states, read by nobody -- and reading
the corpus for an instance found one at once. `S-conforming-fault-from-established` transitions
`unestablished` to `established` and `established` to `faulted` on `fatal-protocol-fault`, never
drains and never closes, and its `sessionEvents` declare `begin-drain creates []` and `close creates
[]` for that session. The vector says one thing to a reader of the timeline and another to a reader
of the events, and every gate was green over it.

Corrected in AX2's form: each declared session event is reconciled against an accepted transition of
its session on that event before any verdict is believed, the vector now declares the
`fatal-protocol-fault` its timeline accepts, and both fields are declared reconciled in the
readership census's table, anchored on the reconciling line so a deleted reconciliation fails as
stale. The reconciliation was seen red on exactly that vector and no other before the corpus was
touched, which is what says the corpus carried one such disagreement rather than several.

### BH2 -- the receiving endpoint was compared only where a record happened to name the step

`delivery[].receivingEndpoint` is read by the step index, which scopes the arrival ordinal by it, and
compared by no property. BF reconciled it against a latch's or a refusal's `recordedBy` -- but only
for the steps such a record resolves to. On every other delivered step, a frame recorded as received
by the endpoint that committed it decided nothing: 16 trials, none decisive.

A frame is committed by one endpoint and delivered to its peer, and a session has two, so the
receiving endpoint restates the committing one and can disagree with it. Corrected in AX2's form:
every delivered step's receiving endpoint is reconciled against its committing endpoint, and the
field is declared reconciled on that anchor. The corpus was consistent -- the reconciliation was
seen green over it and red on a request no record references, which is what the `BH-e` probe keeps
-- so this one is a gap in the verification and not a defect in the corpus.

### BH3 -- the transition event is read into a witness alone, because no artifact routes its tokens

`sessionTimeline[].event` is stated on 140 transition records and read by `S1`, and by `C2-P1`
through it, into the witness alone: 80 trials, 20 moving only the witness, none a verdict. `S1`
judges an accepted transition by its edge, `from>to`, against the legal table the gate cross-checks
with the session state machine. So an accepted transition taking a legal edge on an event the design
does not route there -- `established` to `draining` on `close`, where the grid's Close column at
`established` reads premature close, `faulted` -- is green.

This one is in the design's silence as much as in the evaluator. The machine's legal transition
table keys each row on a From state, a prose event-and-guard cell and a To state; its Events table
names eleven tokens; the coverage grid routes by event **class** and states the routing in prose
cells. No artifact publishes which token routes which row. `S1`'s edge is the unit the brief's
operator set offers -- *transition edges* -- so the evaluator is not wrong against the brief, and a
gate that wrote the token-to-row mapping itself would be a twelfth surface publishing a fact the
design does not, which is the failure W1 exists to retire. The rows that fault from `any
nonterminal` are why it is not a clarification either: whether a premature close at `established`
is the event `close` routed to `faulted` by a detailed row, or a recognized violation routed through
the fault row under the totality rule, is a decision the design has not written down.

So the field is declared inert on the legal table's own header, the reason names the silence, and
the question is put to the owner as open question 5 of the verification foundation plan with the
options and a recommendation. Nothing in the design is changed by this pass, which is why the family
is `verification`; a ruling that publishes the routing is a design change for the pass that lands it.

### BH4 -- under the coverage measure's own cap, the census failed the gate it lives in

The coverage measure runs the properties gate at one (property, input) pair per polarity of each
property, and over the committed instrument it reported that the gate exits 1, so nothing in it could
be measured. The census had reported `sessions[].requiredFacets` and `sessions[].supportedFacets`
inert: each is decisive on a `C11-P1` pair the cap left out and read on the one it walked. A capped
walk is not a census of the corpus, and two of its verdicts are sound only over the whole of it -- a
field decisive only on a dropped pair reads as inert, and a declaration exercised only there reads as
unexercised. The read-provenance census avoids exactly this by feeding its declaration checks from the
uncapped declared loop rather than from itself, and its parameter comment says so; the first draft of
this census did not follow it.

Corrected: both verdicts are drawn from an uncapped run alone, the stale-declaration check stands under
a cap since a field decisive on a walked pair is decisive, and a capped run walks, counts and reports
both with a summary line that says it was capped. Under the measure's arguments the gate now reports
two inert and zero unexercised and passes; uncapped it reports none of either; `BH-a` and `BH-g` were
re-run by hand and still fire.

### BH5 -- the exception-dedupe conditional was reachable only on a failing run and declared nowhere

With BH4 corrected the coverage measure could reach the census for the first time, and on that run it
reported one construct never evaluated: the conditional that keeps one thrown-exception finding per
property, field and exception. It sits inside the branch a throwing evaluator enters, which a passing
run never does, so it is correctly unreachable and owed a declared exemption -- the shape the file's
existing witness-building branches all carry. It is BD3's class exactly: the coverage measure refusing
the pass's own new code on its first run over the branch. Declared, with `BH-f` as the probe that
reaches it.


## What was declared on contact

Four of the eight fields the instrument reported are inert because the design, or a ruling on it,
says so and no finding is raised for them; each is declared with the artifact and the words that
settle it -- checked as BD checks a reader's declaration, and checked against the measure so that a declared field that becomes
decisive fails as stale and one no trial reaches fails as unexercised.

- `declaredSteps[].commitIndex` is reconciled against the declared order at load, where the loader
  has always refused a vector whose two orderings disagree, and `Test-Precedes` reads the declared
  order alone. It was counted as read by the step index; it is declared reconciled on that line.
- `interactions[].authorityRecord.initiatorAttribution` is read by `C6-P1` through `Read-Obligation`,
  because the contract's sentence is that every denial **records** it; the contract fixes no value,
  so the obligation is presence and the value is opaque to the property.
- `interactions[].direction` is read by `C10-P1` for its presence, since the comparison against the
  established profile is `C3-P1`'s and the corpus carries no profile record: the owner ruling of
  2026-09-16 defers that comparison to Batch 2 with `profileMatch` standing in for it.
- `sessions[].establishedProfile` is named in `S5`'s witness and opened by nothing, under the same
  ruling.

The fifth declaration is `sessionTimeline[].event`, which is BH3 above; its reason names the open
question rather than a fact the design states.

## Findings

| id | where | what |
| --- | --- | --- |
| **BH1** | the corpus and the gate, by the instrument's first run | `sessionEvents[].event` and `.session` read by `S6` into its witness alone and reconciled against nothing, and `S-conforming-fault-from-established` declaring `begin-drain` and `close` for a session that faulted from `established`; reconciled against the timeline, and the vector corrected |
| **BH2** | the gate, by the instrument's first run | `delivery[].receivingEndpoint` read by the step index and compared by no property, reconciled against `recordedBy` only where a refusal or latch names the step; reconciled against the committing endpoint on every delivered step |
| **BH3** | the gate and a silence in the design, by the instrument's first run | `sessionTimeline[].event` read by `S1` and `C2-P1` into the witness alone, because no artifact publishes which event token routes which legal-table row; declared inert on the table's header and put to the owner as open question 5 |
| **BH4** | the pass's own code, by the frozen coverage measure | the census's inert and unexercised verdicts failing the gate under the measure's one-pair cap, on two fields decisive only on a pair the cap dropped; both verdicts are now drawn from an uncapped run alone |
| **BH5** | the pass's own code, by the frozen coverage measure | the exception-dedupe conditional, reachable only when an evaluator throws, undeclared; declared with `BH-f` as the probe that reaches it |

## What this pass verified rather than believed

- **The frozen set was run at `ad5a6b4` before any of this work existed**, in a short-path clone, and
  everything reported above was read from that run's output. The deep properties run took 792
  seconds and the coverage measure 1,468, on a machine also running this pass's own gates; the corpus
  figure is in the frozen-set paragraph above.
- **The instrument was made to fail for each of its claimed reasons before its verdict was believed**,
  and seven probes keep the corpus honest about it: `BH-a`, an evaluator keeping a read and testing the
  Boolean for presence rather than truth, reported as read and never decisive beside the declared
  loop's own failure; `BH-b`, an inert declaration on a decisive field, refused as stale; `BH-c`, a
  declaration citing words its artifact does not contain, refused as BD refuses a reader's; `BH-d`,
  the corpus as this pass found it, refused by the session-event reconciliation; `BH-e`, a step
  delivered to its own committer on a request no record references, refused by the receiving-endpoint
  reconciliation alone; `BH-f`, an evaluator that throws on a well-formed wrong value, reported once per
  property, field and exception rather than left as a crash; and `BH-g`, an inert declaration no trial
  reaches, refused as unexercised. Each was run alone in a clone at the instrument commit and seen to
  return the verdict it owes. The corpus goes from 134 to 141.
- **The trials are measured against the baseline the run observed, not against the declaration.**
  The first draft compared each trial's verdict with the declared polarity, and `BH-a` is what showed
  the difference: an evaluator that has stopped using a field reaches the undeclared verdict on its
  own mutation, so every trial on that pair differed from the declaration and the field read as
  decisive. Measured against the observed baseline the field is inert, which is the truth, and the
  disagreement between baseline and declaration is reported beside it.
- **One operand of the pass's own code was found unreachable by reading, before the coverage measure
  saw it.** The mutation function re-tested each list's members for being scalars, and the walker
  that hands it lists already guarantees that, so the third operand of that test could never be
  evaluated. The test was removed and the contract stated; the coverage measure over the committed
  instrument is what says nothing else of the kind remained, and its figures are in the plan's
  section 2y.
- **BH1's instance was found by reading the corpus for what the instrument implied**, and the
  reconciliation was seen red on that vector alone before the vector was corrected. **BH2's
  reconciliation was seen green over the whole corpus and red on the probe.**
- **The whole set was then run over `81eeffd`, the head that records this pass**, in two short-path
  clones: the text, link, design, owned-fact and return-channel gates, the properties gate at its
  default count, the corpus at 141 of 141 over 22 workers in 2,294 seconds, the coverage measure over
  four gates in 1,997 seconds with 46 condition and 6 operand exemptions -- the 41 and 6 it declared
  before this pass plus the five this pass owed, three for loops only a failing run enters, one for
  the census's unexercised-declaration loop and one for BH5's conditional -- and the deep run at
  52,000 evaluations and 0 red with the sweep green over 500, all exit 0. Two earlier runs over the
  branch were not clean and are what BH4 and BH5 record: coverage over `d1d1713` reported the
  properties gate exiting 1 under the measure's arguments, and coverage over `64afa42` reported the
  dedupe conditional never evaluated. The one commit above `81eeffd` changes this paragraph and one
  in the plan's section 2y, and the text, link, design, owned-fact, return-channel and properties
  gates were re-run over it.

## What remains outside the pass

**The census measures the declared corpus, one field at a time.** A field whose value decides only
jointly with another is inert here, and the generated population is not walked: a wrong value on a
path only a generated vector reaches is invisible, as it is to the read-provenance census, and the
sixteenth pass's trade stands unmade for both.

**A wrong value is wrong in kind, not in meaning.** The mutations are drawn from the field's own
classification -- a negation, the set's other members, a sibling identifier -- and a value the design
has no word for is the vocabulary census's subject rather than this one's.

**Per-property inertness is not reported.** The census asks whether a field decides anything for
**some** property on **some** input. A property that reads a field another property decides on, and
never decides on it itself, is a weaker signal this pass counted and did not fail on.

**BH3 is the owner's.** The declaration states the limit; open question 5 states the options.

**The three narratives' finding ranges** are still read by no guard for the range, as the
twenty-first left them.

**Cost.** The operand census adds about 3,540 evaluations to the properties gate: about nine seconds,
the gate at its default count measuring 43 and 46 seconds on the branch against 35 and 36 at `ad5a6b4`
on an idle machine. The corpus gains seven probes at `-GeneratedCount 0`.

**Whether this pass is the first of the two owed is not in question.** The instrument found BH1, BH2
and BH3 in the package on its first run, which is the ruling's second population, and the count
stays at zero; BH4 and BH5 are in the pass's own code and would not have counted either way. The closure
review remains on hold. The finding count by condition-4 pass is now
five, six, three, two, five, one, seven, seven, five, three, one, three, zero, three, seven, seven,
three, three, five, twelve, two and **five**.

## Where this family is dispositioned

No finding reaches a design artifact: BH1 and BH2 are in the properties gate and the corpus, BH3 is
in the properties gate and the verification foundation plan's open questions, and BH4 and BH5 are in
the properties gate and the coverage exemptions. The family is
classified `verification` and is dispositioned in the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md), whose
section 2y carries the pass; this document is its evidence.
