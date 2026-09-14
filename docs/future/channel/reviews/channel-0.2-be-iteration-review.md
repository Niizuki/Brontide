# Channel 0.2 nineteenth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-nineteenth-pass-2026-09-14-0a43917`

Reviewed work: every closed-vocabulary field of every record, against the set the design states for
it and regardless of which property's group the vector is in, at `0a43917`, `Merge pull request #152
from Niizuki/channel-0.2-condition-4-eighteenth-pass`

Date: 2026-09-14

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **nineteenth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not clean.** Its frozen set reported nothing, for the thirteenth consecutive pass. The
instrument it built found **BE1** through **BE5** in the package on its first run, so the ruling's
second test is not met and the two-consecutive count stays at zero. All five are in the verification
-- the corpus, the generator, the property declaration and one evaluator -- and none in the design.
Nothing was raised against the pass's own new code by a frozen instrument; what the pass's own first
run found in its own code is recorded below, unnumbered, because the ruling's populations do not count
it.

The method was not chosen by this pass. The eighteenth named it twice: the narrower unit, every
closed-set field of every record against its set regardless of group, with a known instance waiting
for it -- five interaction records that had carried the provenance form `local-refusal` on vectors no
property reads that field of, corrected on contact and not numbered; and the wider question behind it,
the same question that pass put to the five declarations that carried a reason, put to every field.
The plan's section 2u said to spend the increment on the narrower unit, and this pass did, and built it
so that the walk is total: every string field of every record is classified, and one that is not is a
failure rather than a field the census was never told about.

## Section numbering

**BE1** through **BE5** are what the instrument this pass built reported about the package on its
first run, over the declared corpus and over the generated population together. Nothing carries a
number for the pass's own code.

## The frozen set, run first

**Zero.** In a short-path clone at `0a43917`, before any of this pass's work existed: the text and
link guards, the design gate, the owned-fact gate, the return-channel census, the properties gate at
its default count and at the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors
at 0 red, AZ3's dropped-field sweep over 500 of them with all eight load-bearing droppings reporting
where declared -- the read-provenance census at 990 of 3,714 poisoned fields read and none raw, the
declaration-polarity check, the declaration-citation check, the whole 107-probe corpus at 107 of 107
over 22 parallel workers, and the coverage measure over four gates. Thirteenth consecutive clean
frozen set, and strictly larger than the eighteenth's: it gained the citation check and that pass's
two probes.

It is reported with the caveat every pass since the fourteenth has had to make, and this pass's
findings are that caveat's demonstration. Every instrument in the set asks whether a check runs,
whether a guard fires on its own subject, whether a property is red where it should be green, which
reader performed a read, which polarity exercises a declaration, or whether a cited sentence is in
the artifact. None asks whether a value a record carries is a value the design has. A field that no
property reads, carrying a word no artifact uses, is green under all of them -- and the largest of the
five findings below sits on **every session record in the corpus**.

## The instrument

The properties gate now classifies every string-valued field of every record by its path from the
vector root, and checks each closed-vocabulary field against the set the design states, over every
declared vector regardless of the property group it is in and over every generated vector. A value
outside its set is a failure against the vector, with one exemption: where a property's own clause is
that the value be in the set -- `C9-P1`'s first clause over `provenanceForm` -- a value outside it is
that clause's own red, and it is permitted on exactly the vectors declared red for that property.
Nothing else is exempt, and a generated vector carries no declared verdict, so a generator emitting
outside a set fails outright.

Where a set comes from is declared with it, and the summary line counts each kind:

- **read from an artifact table** -- the session states and the session events are parsed from the
  session state machine's own tables, exactly as S1's transition table is, so the gate restates
  neither. Two of the fifteen;
- **stated by the cited words** -- every member appears backticked in the words the entry cites, and
  the citation is checked as a reader's declaration is under BD: the artifact must be one of the
  design's and must contain the words. Five of the fifteen: effect certainty, the latch value, the
  latch's fault category, and the `unseen` refusal record's provenance and detailed reason;
- **spelled from the cited words** -- each member, or the phrase it declares itself the spelling of,
  appears in the cited words as a whole phrase with each hyphen a space or a hyphen:
  `semantic-outcome` for the contract's *semantic Outcome*, `pre-dispatch` for *before handler
  dispatch*. Eight of the fifteen, and the weakest kind, labelled as such because a spelling is the
  gate's and not the design's.

Three further classes make the walk total. A vocabulary the gate's own harness owns -- a vector's
`role`, a timeline step's `step`, a delivery's `disposition`, the verdicts under `expected` -- is
checked against the gate's list and cites nothing, because it is not a fact the design states. A
vocabulary the design says a **profile** declares -- an interaction's `class` and `direction` -- is
classified and not checked, on the contract's own words that every interaction names one
profile-declared class, and the limit that leaves is stated at the end of this record. And an
identifier or free-text field -- a session id, an interaction identity, a step id, a facet name, a
summary -- is declared as one. A string field that is none of these fails, so a value outside a set
cannot enter the corpus through a field the census was never told about; and a classification no
record of the declared corpus carries fails too, so a key cannot outlive the field it was written
for. The vector file's own top level is refused anything but its vectors, which is where the two
lists this pass deleted had lived.

Over the corrected package it reports **1,020 fields of 56 declared inputs checked against 15 closed
vocabularies, with one out-of-set value on the vector declared red for the property that enforces
the set, 493 harness fields, 80 profile-owned fields and 1,511 identifier fields classified**, and
14,103 vocabulary fields of 100 generated vectors against the same fifteen. The gate's `C9-P1` reads
its four provenance forms from the census's declaration rather than from a list of its own, and `I3`'s
four non-semantic terminal forms are hoisted so the terminal-form set is stated once.

**The first run over the package as found reported 74 findings over the declared corpus and 1,355
over the generated population**, on eight distinct fields. Seven of the eight are the five findings
below; the eighth was the instrument's own -- a generated vector's `role` is `generated-conforming`,
which its harness list did not carry -- and is recorded under what the pass verified.

### BE1 -- every session record stated an initial interaction state the design does not have

Every one of the 58 session records in the corpus, and every session the generator built, carried
`initialInteractionState: idle`. The interaction state machine's initiator states begin at
`candidate` and its recipient states at `unseen`; no artifact in the package uses the word `idle` for
anything but a timeout the session machine says core does not have. The instrument reported it 58
times over the declared corpus and on every generated vector.

**No property reads the field.** It is AX2's shape with a wrong value in it, which is the sentence the
eighteenth pass used to name this unit: a field nobody reads, on every vector, stating a state the
design never declared, and green under every frozen instrument because none of them reads what a
value says. The brief's vector format lists *the established profile and initial session/interaction
state of each session the vector carries*, and the corpus note says these inputs carry only what a
property statement quantifies over. What the design says about the initial interaction state is not a
per-vector fact: every recipient identity begins at `unseen` and no initiator state exists before a
caller proposes an interaction, so a session record stating one value for both endpoints was restating
a constant of the design, under a name the design does not use, for a two-endpoint vector the brief's
own format would give an endpoint perspective to. **The correction deletes the field** from the
corpus and the generator, as BD1 deleted a field that carried a convention of its own; the design's
initial states stay where the machines state them. The census keeps no classification for the path,
so a session record that reappears with the field fails as unclassified until someone says which
design vocabulary it carries -- the `BE-g` probe pins the other half, a classification left behind by
a deleted field.

### BE2 -- the `unseen` refusal record's provenance was a value the design's record does not state, and the property selected on it

C10 owns the `unseen` refusal record, and the fact rendered into all five of its surfaces reads *its
provenance `rejected-protocol`, its detailed reason `unopened-interaction-identity`, its effect
certainty `known-none`, and the refused-frame reference*. The interaction machine says why the word is
provenance: *`rejected-protocol` is the provenance the refusal is recorded under, not a state the
recipient sits in*. The corpus's five records carried `provenance: recipient`, and carried the
design's value under `frameDecision` -- a field the design's record does not have, and a name from the
Channel 0.1 vector conventions. The instrument reported the first as outside the set and the second as
classified as nothing.

**That is the AK1 shape one artifact further out.** The corpus, the property declaration's selector in
`channel-0.2-properties.json`, the evaluator's selector, the generator and the generated-shape check
all agreed with each other exactly -- `provenance` equals `recipient`, `frameDecision` equals
`rejected-protocol` -- and none of the five agreed with the artifact that owns the record. **Pinned
before it was believed, and it is the pin worth reading.** The five records were corrected first and
the selectors left alone, so the corpus stated the record as the design states it: `C4-P2` went
**green on `C4-control-precedes-request`**, its own named mutation, and the AK1 and AK5 operand
mutations went green with it, because the first conjunct's selector skipped every record whose
provenance was not `recipient`. A vector authored from the artifact would have taken the property
green on the mutation twenty-one findings converged on -- U1, reached through the record's own
vocabulary. The correction reads the design's value at every surface: the five records, the
declaration's selector, the evaluator's, the generator's two refusals and the shape check, with
`frameDecision` deleted from all of them. The `BE-j` probe restores the old selector value and
requires the gate to report exactly that green.

### BE3 -- two mutation records carried an effect certainty outside the three

`C5-pre-dispatch-refusal-possible-effect` and `C6-denial-with-possible-effect` recorded
`effectCertainty: possible`. The contract's common terms state the form: *`known-none`, `known`, or
`unknown`, with a reason where unknown*. The properties those vectors mutate go red on any value but
`known-none`, so each was red -- but through a value no realization can record, which makes the
mutation a vector describing a world the design has no word for rather than a realization violating
the design. The correction is `unknown`, the value the design gives a pre-dispatch refusal whose
effects are not known to be none, and both properties are red on it for the reason their clauses
state.

### BE4 -- session events the machine's table does not name, in two mutations and in the generator

`S1-illegal-transition-accepted` accepted `established` to `unestablished` on an event `reset`, and
`S4-terminal-session-resumed` accepted `closed` to `established` on `resume`. The session state
machine's event table has eleven events and neither of those. The generator, read the same way, emitted
`offer-profile` and `accept-profile` for its establishing route and `recognized-violation` for its
fault, where the table says `send-establish-proposal`, `accept-establishment` and
`fatal-protocol-fault` -- so the population every property has reported green over, at 52,000
evaluations, carried three event names the design does not have: one on every session that
established through `establishing` and one on every session that faulted. S1 reads the edge and not
the event, which is why the properties never noticed; it is also why the event name is data a reader
of the record trusts and nothing checks. The two mutations now accept their illegal edges
on `receive-establish-proposal` and `accept-establishment`, inputs the design does recognize and a
conforming realization would leave the state unchanged or fault on, and the generator emits the
table's own names; the `BE-f` probe puts `offer-profile` back and requires the census to report it
over the generated population.

### BE5 -- a clear latch carried a fault category

Two `clear` latches recorded `category: none`. The one fault a latch commits is the interaction-scoped
`state-violation` the interaction machine names when it settles, and a latch that is `clear` has
committed none -- so there is no category to record, and `none` is a value from the same 0.1
conventions `frameDecision` came from. The correction omits the field on a clear latch; `C4-P2`'s
selector treats an absent selector field as narrowing nothing, and the latch value then excludes the
record, so no verdict moves. The alternative -- an explicit absence value, as the brief gives the latch
itself with `not-applicable` -- is the open question the seventeenth pass put to the owner, whether a
vector states an omission positively, and is left with it rather than decided here.

## What was corrected on contact

The vector file declared `frameKinds` and `latchValues` at its top level. Nothing read either: the
gate compared frame kinds and latch values at the evaluators that read them, from literals of its own,
and the two lists were a third statement of facts the design owns. Both are deleted, the census cites
the design for both sets, and the file's top level is refused anything but its vectors. Found by
reading rather than by the instrument, and not numbered for the reason the eighteenth pass gave for
its five `local-refusal` values.

## Findings

| id | where | what |
| --- | --- | --- |
| **BE1** | package | every session record, and the generator, stated `initialInteractionState: idle`, a state no artifact has; the field is read by no property and is deleted |
| **BE2** | package | the `unseen` refusal record's `provenance` was `recipient` where C10's fact states `rejected-protocol`, the design's value was carried under a field the record does not have, and `C4-P2`'s selector agreed with the corpus and not with the artifact -- stated as the artifact states it, the property was green on its own named mutation |
| **BE3** | package | two mutation records carried the effect certainty `possible`, outside `known-none`, `known` and `unknown` |
| **BE4** | package | two mutations accepted illegal edges on the events `reset` and `resume`, and the generator emitted `offer-profile`, `accept-profile` and `recognized-violation`; none is in the session machine's event table |
| **BE5** | package | two clear latches recorded the fault category `none`; a clear latch has committed no fault and states none |

## What this pass verified rather than believed

- **The frozen set was run at `0a43917` before any of this work existed**, in a short-path clone
  rather than the working tree, and everything reported above as green was read from that run's
  output rather than assumed from the previous pass's. The probe corpus took 1,440 seconds there over
  22 workers, against the plan's 427 on 16, on a machine that was also running this pass's own gates;
  the figure is recorded as an observation and not as a re-measurement.
- **The instrument was made to fail for each of its claimed reasons before its verdicts were
  believed**, and ten probes keep the corpus honest about it: `BE-a` a value outside a set on a field no
  property reads on that vector, `BE-b` a vocabulary citing words its artifact does not contain, `BE-c`
  a member none of the cited words name, `BE-d` a string field the census was never told about, `BE-e`
  a closed set declared at the vector file's top level, `BE-f` the generator emitting an event the table
  does not name, `BE-g` a classification no record carries, `BE-h` an out-of-set recorded provenance
  form on `C9-P1`'s own red vector passing whatever the value is, `BE-i` the same exemption **not**
  reaching the vector's expected form, and `BE-j` the selector of BE2. The corpus goes from 107 to 117,
  and the plan's section 4 is corrected in the same change.
- **Both halves of BE2 were pinned by deliberate failure**, in the order that shows the mechanism: the
  records first, so the selectors' agreement with each other and not with the artifact produced the
  false green, and only then the selectors.
- **The instrument's own first run found two defects in the instrument before it found the package's.**
  A citation of the `unseen` refusal record's fact could not be found in C10 because the words span a
  fact fence -- `<!-- fact:unseen-refusal-record -->` sits between *carrying* and *its provenance* --
  and the citation now cites the rendered words inside the fence, which is the text every surface of
  the fact actually carries; and the generator's `role` value was not in the harness list, which the
  generated population reported on the first run and which is the totality rule catching its own
  author. Neither is numbered: neither was found by a frozen instrument, and the ruling's populations
  count what the frozen set and a new instrument report about the package.
- **The coverage measure was run over the corrected package**, and the two report loops a passing run
  leaves empty are declared to it with the probes that pin each, in the form the file uses for the
  generated-findings loop beside them. Every other conditional and operand the pass added is
  evaluated by the passing run: the `Enforced` exemption's two operands are both reached on `C9-P1`'s
  mutation, and the walk's leaf test reaches both of its operands on a string and on a Boolean.
- **The return-channel census sees nothing new to police.** The census walks a vector into a list it
  is handed and adds findings to a list it is handed, returns no record, and declares no `$script:`
  accumulator; the `Seen` set is local to the run that owns it. The census is green over the change.

## What remains outside the pass

**A spelled set is the gate's spelling.** Eight of the fifteen vocabularies are members this file wrote
from a sentence that names the cases in prose, and the check is that the phrase is in the cited words
-- not that the design intends the phrase as an enumerated member, and not that the design's set has
no member the file left out. That is BD's limit at member granularity: presence, not meaning. Where a
design artifact states a set as a table the census reads the table, and the two sets that do come from
tables are the two the gate could not get wrong.

**Two vocabularies are a profile's and the corpus carries no profile.** `class` and `direction` are
classified and not checked, because the contract says every interaction names one profile-declared
class and C3-P1 compares each against *the established profile of its own session*. The corpus's
`relational` is a class name no artifact declares -- the contract's Portable Binding 0.2 profile
declares `relational-initialisation` and `ordinary` -- and `I6` and `C7-P1` select interactions by that
literal, while the neutral profile the corpus's sessions establish declares no classes at all and
`C3-P1` reads a `profileMatch` Boolean the vector asserts rather than the profile. The census cannot
decide a value against a set whose owner is not a record in the corpus; correcting that means the
profile record declaring its classes and directions and `C3-P1` reading them, which changes what a
conforming session record carries and is a larger unit than this pass's.

**The wider question is still open, and a first look at it is recorded.** The classification puts one
question to every field -- what kind of field is this, and where does the design say so -- and not the
question the eighteenth pass named: which fields the brief's format requires against which fields the
corpus states, and which of those any property reads. Three fields the walk classified are read by no
property at all: `initialSessionState`, `establishedProfile` and both `recordedBy` fields. The first is
the one to weigh, because `S2` and `C2-P1` each default a session's state to `unestablished` in code
when no transition has been seen, while twelve session records across eleven vectors state
`initialSessionState: established` and nobody reads them -- one fact, two surfaces, the record's read by nobody, which is
AX2's own shape on the field beside the one BE1 deleted. Measuring "read by no property" is harder than
it looks: the read-provenance census walks green-expected inputs only, so a field read only on a
mutation's path -- `refusal.explicitEvidence` on a post-dispatch refusal -- is read by nobody it can
see. That is the next unit, and it is left named rather than half-built.

**A settled latch's category is a constant of its value.** The only fault a latch commits is
`state-violation`, so `category` on a `fault-committed` latch restates what the latch value already
says. It is left in place because `C4-P2`'s declaration selects on it, and it is one more surface of
the kind W1 retires.

**The generated population still carries one frame shape per session**, and the generator still
produces conforming vectors only. Unchanged since the fourteenth, and unreached by this pass.

**Cost.** The declared-corpus census is a walk over 1,020 vocabulary fields and about 2,100 other
leaves and is unmeasurable beside the read-provenance census next to it. The generated census walks
every generated vector, about 330 leaves each, and at the default count is under two seconds of the
gate. The corpus gains ten probes, nine at the properties gate's `-GeneratedCount 0` cost and one at
fifteen generated vectors.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, seven, seven, three, three,
**five** -- all five in the package and none in the pass's own new code.

## Where this family is dispositioned

**BE1** through **BE5** are corrections to the verification instruments, the declared corpus, the
generator, one property declaration and one evaluator -- not to the design -- so under the 2026-08-20
ruling they belong in the verification foundation plan's own record and not in the completeness
review's disposition index. The plan's section 2v carries them, and this document is the pass's
evidence.
