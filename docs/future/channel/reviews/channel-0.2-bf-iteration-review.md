# Channel 0.2 twentieth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-twentieth-pass-2026-09-14-4557704`

Reviewed work: every field the declared corpus states, against whether anything reads it -- a
property on any input it declares, green-expected or red-expected, the step index, or a declared
reconciliation -- and the generator's emitted surface against the corpus's, at `4557704`, `Merge pull
request #153 from Niizuki/claude/next-item-implementation-06b513`

Date: 2026-09-14

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **twentieth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, and it was to be the **first of the two consecutive clean passes** the 2026-09-04 ruling
requires from zero.

**It is not clean, and for the first time in fourteen passes the frozen set is the reason as well.**
The frozen set reported **BF1**: one probe of the 117-probe corpus, `AQ1-a`, returned pass at
`4557704`, because the guard it pins had gone dark in the commit that recorded the nineteenth pass.
The instrument this pass built then found **BF2** through **BF9** in the package on its first run, so
the ruling's second test is not met either and the two-consecutive count stays at zero. **BF10** and
**BF12** are against this pass's own new code and were found by frozen instruments; **BF11** is
against a frozen instrument and was found by reading. None of the three is in the ruling's two
populations, and all are numbered because the corrections cite them.

The method was not chosen by this pass. The eighteenth named the question and the nineteenth left
it open with a first look: which fields the corpus states that no property reads, against which the
brief's format requires, with `initialSessionState`, `establishedProfile` and both `recordedBy`
fields named as read by nobody and the warning that "read by no property" is harder to measure than
it looks, because the read-provenance census walks green-expected inputs only. This pass walked both
polarities, and the four fields the nineteenth named are four of the eleven it found.

## Section numbering

**BF1** is what the frozen set reported about the package. **BF2** through **BF9** are what the
instrument this pass built reported about the package on its first run. **BF10**, **BF11** and
**BF12** carry numbers because the corrections cite them and belong to neither counted population.

## The frozen set, run first

**One.** In a short-path clone at `4557704`, before any of this pass's work existed: the text and
link guards, the design gate, the owned-fact gate, the return-channel census, the properties gate at
its default count and at the deep run's -- 52,000 evaluations over 2,000 generated conforming vectors
at 0 red, AZ3's dropped-field sweep over 500 of them with all eight load-bearing droppings reporting
where declared, the read-provenance census, the closed-vocabulary census over both populations, the
declaration-polarity and declaration-citation checks -- and the coverage measure over four gates, all
green. The probe corpus was not: **116 of 117**, and the one that failed is BF1 below. The streak of
clean frozen sets ends at thirteen, and it ends on a probe the pass that broke it could have run.

## The instrument

Every field of a **recording copy** of each declared vector is replaced by a getter that records the
field's path and returns the value it replaced, so an evaluator sees exactly what the declared loop
showed it and reaches exactly the verdict it reached there -- which the replay checks on every pair
rather than assumes: verdict, conjunct, error channel and unpublished-field channel, all compared
against the declared loop's. One run per (property, input) pair, over every input a property declares
whichever verdict it declares, records every field that run dereferenced, through a sanctioned reader
or raw. The step index is replayed over the same copy, so a field a property reads through it -- a
declared step's, a delivery's -- is read. A field that nothing reads is a failure against the corpus.

It is not the read-provenance census over the other polarity. That census poisons one field at a
time to ask *which reader* performed a read; this one asks only *whether* anything did, which is why
it costs one evaluation per pair rather than one per field -- about two seconds at `-GeneratedCount
0` -- and can walk both polarities uncapped, under the coverage trace included.

Three classes make the verdict total. A field under one of the vector's **own statements about
itself** -- its id, capability, memberships, role, summary, the finding that raised it, the injection
it declares, its expected verdicts -- is read by the harness or by a person and is declared so, with
the rule BE-g stated: a declared member no vector carries fails. A field the harness **reconciles**
against the surface a property does read is declared with the reconciling line as its anchor, so a
reconciliation deleted with the declaration left standing fails as stale. Everything else is read by
a property or the index, or it is reported.

The generator's half compares the surface it emits against the corpus's: a field the generator emits
that no declared vector states has a readership nothing measured and fails, so a convention of the
generator's own -- BB2's `admitted` event -- cannot enter the population through a field the census
never saw.

Over the corrected package it reports **147 distinct fields stated by 59 declared vectors, replayed
over 137 (property, input) pairs of both polarities -- 85 read by a property on a green-expected input,
10 read only on a red-expected input, 12 read by the step index alone, 5 reconciled, 35 the vector's
own statements, and 0 read by nothing**; and 113 fields emitted by the generator, every one stated by
the corpus.

**The first run over the package as found reported eleven fields, stated 285 times across the corpus, read by nothing.**
They are the eight findings below, four of them the fields the nineteenth named.

### BF1 -- the AJ2 narrative guard's ordinal key was satisfied by "thirteenth consecutive", and the pass that wrote it merged without running the probe that says so

`AQ1-a` removes "thirteenth" from the sentence introducing closure review 13 in the future-work
index's narrative and requires the design gate to fail. At `4557704` it passed. The AJ2 guard's key
was `\bthirteenth\b(?!\s+pass\b)`: AX3 had found the bare ordinal satisfied by "a thirteenth pass is
the next work" and excluded the noun `pass`. The nineteenth pass's narrative then counted clean frozen
sets -- *thirteenth consecutive* -- and the exclusion did not cover the new noun, so the sentence the
guard exists to protect could be deleted with the gate green. The same holds for the ninth through
twelfth reviews, whose ordinals the same narrative now uses for streak counts.

**Two things are the finding.** The guard is AX3's class a fourth time, after AU3 and AV3: a key that
names what does *not* count expires with every new use of the word. And the corpus that reports it was
not run: the nineteenth pass's pull request says the full corpus over its branch was "not yet run" and
that the request "is not ready to merge until they report", and it was merged. That is the case
`verify-gate-self-checks.ps1` states in its header -- a correction can now rot a probe and merge, and
a rotted probe found later is a finding against whoever merged past it.

**The correction names what does count.** An ordinal introduces a closure review when the word
`review`, optionally qualified `independent` or `closure`, follows it directly, or when a bold finding
token of *that review's own family* follows within eighty characters -- which is how the Channel index
names each review by what it raised, "the tenth raised **AF1**-**AF8**". A streak count, a pass and a
link to an iteration review satisfy neither. All thirty narrative cells the check reads were run
against the new key before it was believed; `AQ1-a` is red on the mutation and green on the package,
and the `BF-m` probe records that the Channel index's own phrasing still passes.

### BF2 -- the interaction's direction is on C10's list and was read by nothing

C10 says every interaction's local observation is *sufficient to distinguish profile, session and
interaction identities, direction, class, admission and authority decisions* and the rest of its list.
Thirty interaction records stated `direction`, and no property read it: `C3-P1`, whose statement
compares class, direction and phase against the profile, reads a `profileMatch` Boolean the vector
asserts, because the corpus carries no profile record to compare against -- the limit BE recorded. Every
other item on C10's list is read by some property; the direction was the one that reached no evaluator
through any surface.

`C10-P1`'s first clause is *every observation is complete for its provenance form*, and it read a
second asserted Boolean, `observationComplete`. It now reads the direction itself, through
`Read-Obligation` citing C10's words: an observation that omits it is not sufficient to distinguish
it, whatever the Boolean asserts. `C10-observation-omits-direction` is the mutation, registered in the
completeness review's audit row, and the obligation's polarity is checked by the frozen BC machinery.
What stays open is the comparison itself, which needs the profile record.

### BF3 -- `S2` and `C2-P1` defaulted the state before a session's first transition to `unestablished` in code, while sixty records stated it and nobody read them

This is the one to weigh. The brief's vector format says every vector states *the established profile
and initial session/interaction state of each session the vector carries*, and the corpus does: every
C4 vector carries a session that begins `established` with no establishment in its timeline, because
its window opens mid-session. `S2` and `C2-P1` each began every session at `unestablished` by a
literal in the evaluator. So **a conforming realization whose window opens mid-session was red on
both** -- `S-established-at-start`, a session that admits, dispatches and closes one interaction from
`established` with no transition, is a legal input by the brief's own format, and both properties
reported it dispatching "while its own session was unestablished". That is AE1's class, a property red
on legal behaviour nobody had written down, and it survived because no vector in either property's
group had ever stated anything but `unestablished`.

Both evaluators now read the state the session record states, through `Read-Required`, so a session
the vector carries no record for is silent on the fact rather than defaulted. `S-established-at-start`
joins both properties as an additional-green member, and the `BF-j` and `BF-k` probes restore each
literal and require each property red on it. The record and the timeline's first transition are two
statements of one state, so the harness reconciles them: a first transition departing from any state
but the stated one fails against the vector, which `BF-i` pins.

### BF4 -- the `unseen` refusal's effect certainty is fixed by C10's fact and was read by nothing

C10's owned fact states the record's provenance, detailed reason, effect certainty `known-none` and
refused-frame reference. BE made the first two one-member vocabularies; the third was left under the
three-member certainty set, so a refusal at `unseen` recording `unknown` was inside a set the design
states and outside the fact the design states, with every gate green. No property read it: `C4-P2`
selects on the two beside it, and `I4` -- *every pre-dispatch refusal is `known-none`* -- read
interaction refusals only, though a refusal at `unseen` dispatches nothing and is the other pre-dispatch
refusal the design has.

`I4`'s first clause now quantifies over the record C10 owns as well, through the same words.
`I4-unseen-refusal-not-known-none` fires through it and is registered in the audit row; the conforming
refusal at `unseen`, `C4-control-for-unopened-identity`, joins `I4`'s inputs so the clause is observed
staying green and not only going red on its own mutation, which is BC2's lesson; and the field is a
one-member vocabulary in the closed-vocabulary census, checked on every vector whatever group it is in,
with the `BF-l` probe putting `unknown` on a vector `I4` is not declared over. `I4`'s five inputs gain
an empty observation block, because a property that quantifies over a record reads the collection it
lives in.

### BF5 -- the timeline's terminal steps restated the interaction's terminal history, on eighty-four fields nothing read

Every terminal step of every timeline carried `form` and `semanticSuccess`, and the interaction record
it closes carries the same two on its terminal history, which is where `I3`, `C8-P1` and `C10-P1` read
them. The generator kept the two coherent by construction and said so in its own comment; nothing
checked the corpus's coherence, and nothing read the timeline's copy. One fact, two surfaces, one of
them read by nobody -- W1's class on the corpus. The timeline's copy is deleted, from the forty-two
steps and from the generator, and the closed-vocabulary census stops classifying the path it carried.

### BF6 -- the latch's own session and identity restated its terminal-frame reference's

A late-traffic latch is one terminal interaction's, and C10 says its terminal-frame reference names
*that interaction's own frame*; the latch's own `session` and `interactionIdentity` are the reference's,
restated, and `C4-P2` reads the reference. Reconciled in AX2's form: a latch whose own identity
differs from its terminal-frame reference's fails against the vector, and the field is declared
reconciled with the reconciling line as its anchor. `BF-f` pins it.

### BF7 -- both `recordedBy` fields are the endpoint the delivery says received the frame

The contract says the committing endpoint of the frame a refusal or latch names *is never the
endpoint that records it*: a latch settles against a frame its endpoint received, and a refusal at
`unseen` is the receiving endpoint's own observation. So a settled latch's recorder is the receiving
endpoint of the step its settling-frame reference resolves to, and an `unseen` refusal's is the
receiving endpoint of its refused frame -- both stated by the delivery, and both reconciled now, with
`BF-g` and `BF-h` pinning each. **The limit is stated where it applies:** a `clear` latch settled
against nothing and names no received frame, so on the two such records the field has no second
surface and is what a reader of the record sees.

### BF8 -- the per-interaction `deterministicExpectedObservation` restated a fact of the vector

`C12-P1` is *every neutral vector has one deterministic expected portable observation*, and it reads
the vector-level field. Thirty interaction records carried a copy, read by nothing. Deleted from the
corpus and from the generator.

### BF9 -- the declared profile is required by the brief and was read by nothing

`sessions[].establishedProfile`, on every session record, is the profile the brief's format says every
vector states for each session, and `S5` is *fixed and negotiated establishment of that session's own
declared profile produce equal normative profile records*. `S5` compared the two records and never
named the profile they were establishing. Its witness names it now, which is a read on the red
polarity only and is recorded as exactly that: the property's operand named in its witness, compared
against nothing. The comparison the design asks for -- an interaction's class and direction against
the profile's own declaration -- is the larger unit BE left open and this pass leaves open.

### BF10 -- the new session-record read refused a poisoned session id

`Get-InitialSessionState`, the helper `S2` and `C2-P1` read the stated initial state through, took its
session id as a mandatory string, and the frozen read-provenance census poisons the timeline event's
`session` field to an empty one on its walk -- so the first run of the changed gate threw inside the
census rather than reporting. Against this pass's own new code, found by a frozen instrument on the
first run, corrected by allowing the empty string; the census then reported the read through a
sanctioned reader as it should.

### BF11 -- the design gate's next-pass ordinal check would have gone silent on this pass

The design gate recomputes the number of retained condition-4 passes and checks the sentences naming
the next one. Its word lists ended at `twentieth`, and the two branches treated running off the end
differently: the cardinal branch fails loudly, and the ordinal branch set the next ordinal to `$null`
and **skipped the check** -- so the pass that retained the twentieth review would have been the first
whose "next work is the twenty-first" sentences nothing read, with the gate green. AP1's class inside
the guard written against AP1's class, found by reading the guard this pass had to extend rather than
by any instrument. The lists reach thirty and an absent ordinal fails as the cardinal does.

### BF12 -- the reconciliation anchors carried a logical operator the coverage measure rewrites

The reconciliation declarations anchor on the reconciling line, and the check reads the file it is
running from, so a reconciliation deleted with its declaration standing fails as stale. The coverage
measure's operand unit -- frozen since the eighth pass -- runs the properties gate from a copy in
which every `-and`/`-or` operand is wrapped in a recording call, and two of the four anchored lines
carried an `-and`. The copy no longer contained them, and the gate failed under instrumentation where
it passes without, which that measure reported on its first run over the branch as exactly the
defect class it names for itself. Against this pass's own new code, found by a frozen instrument;
the two lines are split so the anchored comparison carries no logical operator, and the declaration
says why.

## What was corrected on contact

`BE-a` pinned an out-of-set certainty on the first `effectCertainty` in the corpus, which is an
`unseen` refusal's; that field is a one-member vocabulary now, so the probe's expected message names
the new set. The return-channel census's producer exemption for the generator said the question of
which declared field no property reads "is answered by the reconciliation the vector index performs",
which was true of one field; it now says the field-readership census answers it. And the probe
corpus's `gate` field takes a file name and not a path, which the harness said on the first run.

## Findings

| id | where | what |
| --- | --- | --- |
| **BF1** | package, by the frozen set | the AJ2 narrative guard's key `\bthirteenth\b(?!\s+pass\b)` was satisfied by the nineteenth pass's "thirteenth consecutive", so `AQ1-a` returned pass at the head; the key now names what introduces a review, and the pass that broke the probe had merged with the corpus unrun |
| **BF2** | package | `interactions[].direction`, on C10's list of what an observation must distinguish, stated on thirty records and read by no property; `C10-P1` reads it as an obligation |
| **BF3** | package | `sessions[].initialSessionState`, stated on sixty records and read by nothing, while `S2` and `C2-P1` began every session at `unestablished` in code -- red on a legal input whose window opens mid-session; both read the record |
| **BF4** | package | `observations.unseenRefusals[].effectCertainty`, fixed at `known-none` by C10's fact, read by nothing and inside the three-member set; `I4`'s first clause reads it and it is a one-member vocabulary |
| **BF5** | package | `sessionTimeline[].form` and `.semanticSuccess`, the interaction's terminal history restated on eighty-four fields nothing read; deleted |
| **BF6** | package | `observations.lateTrafficLatches[].session` and `.interactionIdentity`, the terminal-frame reference's restated; reconciled |
| **BF7** | package | both `recordedBy` fields, the receiving endpoint of the frame each record names; reconciled where a received frame is named |
| **BF8** | package | `interactions[].deterministicExpectedObservation`, the vector's own fact restated per interaction; deleted |
| **BF9** | package | `sessions[].establishedProfile`, brief-required and read by nothing; `S5` names it, and the comparison stays the larger unit |
| **BF10** | this pass's own code | the session-record read refused a poisoned session id, thrown by the frozen read-provenance census on the first run |
| **BF11** | a frozen instrument, by reading | the design gate's next-pass ordinal check skipped silently past the twentieth pass while its cardinal twin failed loudly |
| **BF12** | this pass's own code | two reconciliation anchors carried an `-and` the coverage measure's operand unit rewrites, so the instrumented gate no longer contained its own anchors and failed where it passes uninstrumented |

## What this pass verified rather than believed

- **The frozen set was run at `4557704` before any of this work existed**, in a short-path clone,
  and everything reported above was read from that run's output. The probe corpus took 1,115 seconds
  over 22 workers on a machine also running this pass's own gates; the coverage measure 778 seconds; the
  deep properties run 502 seconds. `AQ1-a` was then re-run alone in the same clone and reproduced.
- **The instrument was made to fail for each of its claimed reasons before its verdicts were
  believed**, and thirteen probes keep the corpus honest about it: `BF-a` a field that loses its last
  reader, `BF-c` a reconciliation deleted with its declaration standing, `BF-d` a self member no vector
  carries, `BF-e` the generator emitting a field the corpus never states, `BF-f` through `BF-i` the
  four reconciliations, `BF-j` and `BF-k` each evaluator's restored literal red on the legal input,
  `BF-l` the one-member vocabulary on a vector its property is not declared over, and `BF-m` the AJ2
  key still accepting a review introduced by what it raised. `BF-b` is an expect-pass record of a
  limit: the getter returns collections through a unary comma so the copy publishes exactly what the
  vector published, and no reader in the suite tells a scalar from a one-element list, so removing the
  comma is not caught. The comment that claimed otherwise was corrected to say so. The corpus goes from
  117 to 130.
- **The replay's self-check is what says the recording measured what it claims.** Every one of the
  137 pairs reaches the declared loop's verdict through the declared loop's conjunct with both channels
  empty; a wrapped subtree renders differently under `ConvertTo-Json`, which `Read-Rendering` reads,
  and the check is what says `S5`'s comparison is unaffected rather than this record.
- **BF3 was pinned by deliberate failure**, through `BF-j` and `BF-k`: with each evaluator's literal
  restored, the property is red on `S-established-at-start` for the reason the finding states, and
  each probe was run alone and seen to return that verdict before the finding was believed.
- **The coverage measure, the whole probe corpus and the deep properties run over the branch were run
  in the clone before this record was finished**, and two of the three reported. The coverage measure
  reported BF12 on its first run, which is recorded above in the way BD3 was, and could not measure the
  design gate while that gate was red for the records this pass had not yet written. The corpus
  reported four: `BF-m` and `A3 AM1` are expect-pass probes on the design gate and fail while it is
  red for the same reason; `BF-e` was written as a two-line edit to a `.ps1` file, which the clone
  checks out with CRLF line endings, so its anchor could not match -- AZ4's lesson, the probe is one
  line now; and `AO2-a` anchored on the plan's section 2a counts, which this pass's three inputs
  moved, and is re-anchored. The deep run was green: 52,000 evaluations at 0 red. All three are run
  again over the final branch head before the pull request is opened, and the result is recorded there.
  Every conditional and operand the pass added was written to be evaluated by a passing run; the three
  report loops a passing run leaves empty are declared to the measure with the probes that pin each.
- **The return-channel census sees the new dispatch and passes.** The replay reads every member the
  evaluator hands back and clears and drains the per-evaluation accumulator; the census reports twenty
  consumers where it reported nineteen.

## What remains outside the pass

**A read is a dereference, not an operand.** A field read only into a witness string is read here,
and BF9 is exactly that. Whether a value can move a verdict is the dropped-field sweep's question,
asked over frame references and nowhere else; asking it of every field is the operand-mutation unit
generalised, and it is named rather than half-built.

**A read on a red-expected input is recorded without asking which reader performed it.** Ten fields
are read only where a property is declared red, and the read-provenance census cannot see those reads
-- its first declared limit, `BB1-c`. A raw read on such a path is a read here and a raw read
nowhere. The census's poisoning walk over the red polarity is the instrument that would close it.

**Two fields are the vector's own statements that no harness code reads either.** `summary`,
`raisedBy` and `reorderingInjection` are read by a person, and `reorderingInjection` is what the
brief requires the two C4 mutations to declare; the census declares them rather than measuring them.

**The comparison behind BF2 and BF9 is still the profile record.** `C3-P1` reads `profileMatch`,
`C10-P1` still reads `observationComplete` for every item of C10's list but one, and `S5` names a
profile it cannot open. Correcting that changes what a conforming session record carries and is the
owner-sized unit BE named.

**Cost.** The census is about two seconds of the gate at `-GeneratedCount 0`, one recording copy per
vector shared across the properties that replay it. The corpus gains thirteen probes, twelve at the
properties gate's `-GeneratedCount 0` cost, one at fifteen generated vectors.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, seven, seven, five, three, one, three, zero, three, seven, seven, three, three, five,
**twelve** -- one by the frozen set, eight by the new instrument in the package, two in the pass's own
code, and one in a frozen instrument by reading.

## Where this family is dispositioned

**BF2** and **BF4** reach a design artifact: the completeness review's per-capability audit rows for
`C10` and `I4` name the two mutations, on AR's, AT's and AU's precedent, so the family is classified
`design` whole and is dispositioned in the completeness review's review-disposition history. The plan's
section 2w carries the pass, and this document is its evidence.
