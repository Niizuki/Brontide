# Channel 0.2 design-foundation reviews

Status: four owner rulings resolved; sixteen retained independent reviews, fifteen negative and one
conforming with nonblocking findings — which under the 2026-08-15 closure-standard ruling did not
close the batch. The sixteenth raised blocking **AL1** and **AL2** with nonblocking **AL3** and
**AL4**, the fifteenth raised blocking **AK1** with nonblocking **AK2**-**AK4**, the
fourteenth raised blocking **AJ1** with nonblocking **AJ2**-**AJ7**, and the
thirteenth raised blocking **AI1** with nonblocking **AI2**-**AI9**. B1-B4, N1-N3,
F1-F3, D1-D5, T1-T4, R1-R3, and S1-S3 are closed in the artifacts they were raised against, the
eighth review having re-verified every one of them individually rather than taking closure from an
index. That review's blocking **U1** — the S1 correction gave intra-interaction frame order an owner
but attached `C4-P2` to it, and `C4-P2` quantified over the frames a recipient *accepts* while the
design refuses every reordered frame, so the accepted sequence was empty and the property stayed
green on its own named mutation — is corrected by restating `C4-P2` over the refusal a reordering
produces. Nonblocking **U2**-**U8** are now also corrected, as are **V1** and **V2**, which the
[U1 correction iteration review](./channel-0.2-u1-correction-iteration-review.md) raised against the
U1 correction itself, **W1**-**W6**, which two further iteration passes raised against those
corrections in turn, **X1**-**X7**, which a third raised against the W corrections, **Y1**-**Y4**,
which a fourth raised against the X corrections, **Z1**-**Z4**, which a fifth raised against those,
and **AA1**-**AA3**, which a sixth raised against the entry points rather than the design package,
and **AB1**-**AB2**, which a seventh raised against the two artifacts the others never opened, and
**AC1**-**AC4**, which an eighth raised by evaluating `C4-P2` rather than reading it, and
**AD1**-**AD3**, which a ninth iteration pass raised against the retained records rather than against
the design. The ninth independent closure review then raised blocking **AE1** with nonblocking
**AE2**-**AE5** and ruled the open AD2 call a defect, and the tenth raised blocking **AF1** with
nonblocking **AF2**-**AF8** against the AE corrections, and the eleventh raised blocking **AG1** with
nonblocking **AG2**-**AG5** against those, and the twelfth returned
`conforms-with-nonblocking-findings` with **AH1**-**AH6** and no blocking finding, and the thirteenth
raised blocking **AI1** with nonblocking **AI2**-**AI9** against the AH corrections, the
fourteenth raised blocking **AJ1** with nonblocking **AJ2**-**AJ7** against the AI corrections, and
the fifteenth raised blocking **AK1** with nonblocking **AK2**-**AK4** against the AJ corrections. The
correction pass for AK then enumerated `C4-P1` and `C4-P2` completely instead of sampling one operand,
and raised **AK5**-**AK8** against the design by that audit. The sixteenth raised blocking **AL1** and
**AL2** with nonblocking **AL3** and **AL4** against the AK corrections: **AL1** is a property of the
session state machine reading one session's own drain transition across a vector that may carry two,
**AL2** is the refused-frame reference published in five surfaces and corrected in four, **AL3** is
the declared fact list that made AL1 unreachable by the check written to catch its class, and **AL4**
is the same quantifier defect on `S5`. Every
finding this programme has recorded is
now closed in the artifacts it was raised against — a claim a reviewer should verify against each
finding's own evidence sentences rather than against this paragraph, since **AI9** established that it
was false for six cycles while every entry point asserted it, and **AJ1**, **AJ2**, **AJ3**, and
**AJ4** are four more findings whose disposition said closed while an artifact their evidence named
was untouched. That piece of named residual work is closed. Twenty-five of the twenty-six properties owed the required-green set **AE3** made a normative field, `I1`-`I7` owed a named mutation as well, and only `C4-P2` had either. All twenty-six now state a required-green set and a named mutation, and all twenty-six execute in `build/verify-channel-0.2-properties.ps1`. The counts were twenty-five and eleven until **AK3**, both being counts of the audit's twelve capability *rows* rather than of properties, and the property the first count dropped was `C4-P2`. Two limits are stated rather than closed: `C4-P1` and `I5` carry sets scoped to the one named profile, because the direction scope of the in-flight bound is undecided for a profile in which both endpoints initiate, and C12-P1's third clause is delegated to the repository's dependency guards rather than evaluated over a vector. None of that is a verdict: a
fresh independent closure re-review is pending, and it is the only thing that can close the batch.

The U-through-AC sequence is worth reading before the next review rather than after it. S1 found that the
fact had no owner; U1 that the property carrying it could not fail; V1 that the property's subject was
not compared; V2 that the mutation carrying the property could not be run; W1 that the property could
not be written in the declared language; W3 that half of it had no mutation at all; W4 that its
expected observation rested on a retention rule no artifact stated; W5 that the operator W1 added had
no operand in the vector schema; W6 that its other conjunct read a fact required as evidence and
excluded from comparison; X1 that the fact W6 then made comparable was not the fact the conjunct
reads; X3 that the state machine which is the detailed authority never routed the event at all; X4
that W3's second mutation reached no required vector group; X5 that W4 abolished the record the first
conjunct quantifies over; Y1 and Y2 that neither the capability owning observation nor the schema
holding it carried what X1 and X5 had just made the property read; Y3 that the state X3 routed to was
terminal and therefore latched after all; and Y4 that the settling frame X1 added could not be told
apart from a duplicate terminal, which the property must leave green; and Z1-Z3 that each of those
three fixes in turn depended on something no artifact said. Twenty layers, each of which existed to
guarantee the one above it, and each of which had its own hole. Every one was found by asking what the
previous fix *depends on* rather than whether it is worded correctly, and none was found by re-reading
the contract. The reviewer should assume a further one exists and hunt it that way, rather than
checking whether the rest are now right — and **AC1** is the caution against reading even that method
too narrowly. It is not the layer under the most recent fix; it is the layer under Y4, five families
back, and it was invisible to the six passes that ran after Y4 because each was auditing the artifact
its predecessor had edited rather than the artifacts that own the fact. It was found by evaluating the
property instead — which is what this document asks the closure reviewer to do, and what no iteration
pass before the eighth had done. **Z4**, **AA1**-**AA3**, and **AB1**-**AB2** are the same
warning against reading the sequence too narrowly. Z4 is a requirement missing from the ledger's
inventory, the AA family and AB1 are six cycles of staleness in the three entry-point documents, and
AB2 is S1 itself reappearing at the same place in the same artifact once three corrections had made
the observation record load-bearing. None was introduced by any correction, and every pass since S1
walked past all of them while auditing each other's fixes. The layer under a fix is where these
findings have been, but it is not the only place they are. **AD1** is the third direction, and the one
no pass had tried: audit the retained records against the artifacts rather than the artifacts against
their records. It found the AC pass denying that the AA and AB evidence existed and referring the
resulting non-gap to the owner — so a reviewer that trusts a roster entry, a scope line, or a residual
note over the document it describes will reproduce the defect that produced it. R1 and S1 each required their own
dated owner ruling, recorded in the redesign plan; both are correction rulings and neither joins the
four first-batch rulings, which remain the fixed set recorded on 2026-08-11. The U1 correction needed
no ruling: it was a property that could not fail, which is a defect rather than a choice.

The cycle is deliberately unnamed. Reviews are numbered, not titled, and every artifact says it awaits
"a fresh independent closure re-review" whichever cycle is current — that phrase is the T4 correction
and must survive each round rather than being escalated.

Review cycles are numbered from here rather than named. The first four were called "closure",
"final closure", "definitive closure", and "totality closure", and that escalation is what produced
T4: three artifacts were left naming a cycle that had already run. Every artifact now says it awaits
"a fresh independent closure re-review", and the design verifier rejects a status block that names a
superseded cycle.

This directory retains independent attestations for the complete Channel 0.2 first-batch design
foundation. A review is independent only when its reviewer identity differs from the design author,
it runs in a fresh isolated context at one pinned commit, and it has no access to the author's private
reasoning.

## Two kinds of review

The independence requirement exists to keep a *closing* judgement free of the bias that made the
defect invisible in the first place. It was never a rule against working on the design. Conflating
the two cost this programme a cycle, so the distinction is now explicit.

**Iteration review.** An author-side pass over work in progress. It may share an actor and a context
with the correction it examines, may iterate as many times as there are findings, and may correct
what it finds in the same pass. It is retained as evidence, is named `*-iteration-review.md` rather
than `*-attestation.md`, and states its own non-final status. It **cannot** close the first batch,
cannot authorize Batch 2, cannot produce the closure record, and its verdict is never the conforming
verdict the Closure section requires — however clean it is. Its value is that it finds and fixes
defects cheaply, before a fresh reviewer spends its one shot of cold context on them.

**Independent closure review.** The judgement that can close. It runs in a fresh isolated clone at one
pinned commit, under a reviewer identity distinct from every retained reviewer and from every
correction author, with no access to author private reasoning and no history of having worked on the
artifacts. Only this kind produces an attestation, and only a conforming one opens Batch 2.

Iterating in one context is therefore encouraged for as long as findings remain. What may not happen
is an actor declaring its own work finished. Marking the batch closed is reserved to the independent
closure review, and an iteration review that reports no findings means the work is ready *to be
reviewed*, not that it passed.

## Finding family provenance

Every finding family this programme has recorded, classified by which kind of review raised it. A
family classified `iteration` must have a retained iteration review recording its findings; a family
classified `closure-review` was raised by a numbered independent review and lives in that review's
attestation.

Every family also declares what it was raised **against**, and that axis decides which record owes its
disposition. A `design` family is dispositioned in the completeness review's disposition history, which
is the artifact a reviewer of the design reads. A `verification` family — one raised against the gates,
the declarations, or the verification foundation plan rather than against any design artifact — is
dispositioned in that plan, which is the record that owns that work. **Neither is exempt**, and the
design verifier requires each class in its own home.

The classification is **declared here rather than inferred from the prose below**, and the design
verifier requires it to be total on both axes: every family the policy names must appear in this table
with a `Raised by` and a `Raised against` value, and every `iteration` family must have its retained
record. AD2 was ruled a defect because a comment claimed a
class over code that tested two literals; its replacement derived the class from one sentence shape in
the next-work steps and missed `V` entirely along with `W5` and `W6`, which is AF6 — the same defect
an order of magnitude smaller. A declared, totality-checked table is what removes the wording
dependency instead of narrowing it.

| Family | Raised by | Raised against | Record |
| --- | --- | --- | --- |
| B | closure-review | design | original design-foundation attestation |
| N | closure-review | design | first closure attestation |
| F | closure-review | design | final closure attestation |
| D | closure-review | design | definitive closure attestation |
| T | closure-review | design | totality closure attestation |
| R | closure-review | design | closure re-review attestation |
| S | closure-review | design | closure review 7 attestation |
| U | closure-review | design | closure review 8 attestation |
| V | iteration | design | U1 correction iteration review |
| W | iteration | design | W correction iteration review |
| X | iteration | design | W correction iteration review, first pass |
| Y | iteration | design | W correction iteration review, second pass |
| Z | iteration | design | W correction iteration review, third pass |
| AA | iteration | design | W correction iteration review, fourth pass |
| AB | iteration | design | W correction iteration review, fifth pass |
| AC | iteration | design | AC correction iteration review |
| AD | iteration | design | AD correction iteration review |
| AE | closure-review | design | closure review 9 attestation |
| AF | closure-review | design | closure review 10 attestation |
| AG | closure-review | design | closure review 11 attestation |
| AH | closure-review | design | closure review 12 attestation |
| AI | closure-review | design | closure review 13 attestation |
| AJ | closure-review | design | closure review 14 attestation |
| AK | closure-review | design | closure review 15 attestation (AK1-AK4); AK5-AK8 raised by the AK correction pass and recorded in the completeness review's operand enumeration |
| AL | closure-review | design | closure review 16 attestation |
| AM | iteration | verification | W1-W3 verification-foundation iteration review |
| AN | iteration | verification | second W1-W3 verification-foundation iteration review |
| AO | iteration | verification | third W1-W3 verification-foundation iteration review |
| AP | iteration | verification | fourth W1-W3 verification-foundation iteration review |
| AQ | iteration | verification | fifth W1-W3 verification-foundation iteration review |
| AR | iteration | design | sixth W1-W3 verification-foundation iteration review |
| AS | iteration | verification | seventh W1-W3 verification-foundation iteration review |
| AT | iteration | design | eighth W1-W3 verification-foundation iteration review |
| AU | iteration | design | ninth W1-W3 verification-foundation iteration review |
| AV | iteration | verification | tenth W1-W3 verification-foundation iteration review |
| AW | iteration | verification | eleventh W1-W3 verification-foundation iteration review |
| AX | iteration | verification | twelfth W1-W3 verification-foundation iteration review |

**Owner ruling, 2026-08-20 — why the second axis exists, and what was rejected.** Until AM every family
had been raised against the design, so one ledger served both populations. AM1-AM3 were raised against
the verification work under the hold — a boundary in the design verifier and two measures in the plan —
and recording them in the completeness review's disposition history made the *newest family* one that
had touched no design artifact. That anchor drives five freshness checks: the Channel index's stated
correction range, the future-work index's Channel row, the Design reviews row, every per-artifact
section of the disposition index, and the status-block pointer check. All five were then satisfied by
nine sections saying "unchanged by AM", which is a guard becoming a formality rather than a guard
finding something.

The rejected alternative was to exempt such a family from dispositioning altogether. That is an escape
clause, and **AH4** and **AJ5** are two escape clauses this policy has already had to close; a third,
classified by the author of the finding, in a programme whose recurring defect is an author mis-scoping
their own work, would be a step backwards. What is adopted instead moves the obligation rather than
removing it, and adds a mechanical backstop: a family declared `verification` may not be named by any
design artifact, because a finding whose correction reached the design is a `design` family whatever
its author called it.

**The AK row is the one mixed entry and it is disclosed rather than smoothed over.** Closure review 15
raised **AK1**-**AK4**. **AK5**-**AK8** were raised by the correction pass for AK1, which enumerated
`C4-P1` and `C4-P2` completely rather than sampling one operand as the four cycles before it had, and
they are in the design rather than in a pass over a correction. They have no separate
`*-iteration-review.md` because they are not a review of the AK correction; they are what correcting
AK1 properly turned out to require, and their durable record is the operand-enumeration table the
correction added to the completeness review — a table a reviewer can check row by row, which is
stronger evidence than a narrative of the same work. A reviewer that disagrees with that reading
should say so: the alternative is that an author-side audit inside a correction pass owes its own
retained record, and this row is where that question is visible.

## Required review scope

The reviewer reads and assesses:

1. `Brontide-Architecture-Status.json` and the current Architecture 0.8 document, including its
   Complete Draft/non-ratified status;
2. both stacks' local Architecture 0.8 targets and public-boundary limitations;
3. the Channel 0.2 redesign plan;
4. retained Channel 0.1 design, contract, requirements ledger, 24 vectors, Portable Binding neutral
   schemas, and PB8 closure findings;
5. Decision 13 and its exact CM3/CM4 relational lifecycle requirements;
6. C1-C12, including every named scenario, capability-wide property, evidence mode, and explicit
   silence;
7. both state machines and all legal/illegal/terminal paths;
8. the closed state/event coverage grid, including every catch-all and late-traffic latch;
9. every responsibility-matrix owner and neutral crossing artifact;
10. the completeness review, including its residual risks;
11. every migration-ledger inventory and disposition; and
12. the neutral contract/vector brief and Batch 2 entry gate.

## Required verdicts

An attestation records:

- reviewer identity, reviewed commit, date, and isolation method;
- overall `conforms`, `conforms-with-nonblocking-findings`, or `does-not-conform`;
- one verdict and rationale for each C1-C12;
- session-state, interaction-state, state/event-totality, responsibility, completeness,
  migration-coverage, and neutral-brief verdicts;
- confirmation that each of the four resolved owner rulings is represented consistently throughout
  the first-batch design;
- every blocking and nonblocking finding with exact artifact/section evidence; and
- checks/probes performed, including at least one attempt to falsify a capability-wide property.

The reviewer writes only its requested attestation. It does not repair the design it reviews. A
blocking finding is corrected test/contract-first in a later commit and receives a fresh re-review;
the original negative attestation remains retained.

## Closure

Batch 2 may begin only after:

- architecture owners record the four first-batch rulings;
- every blocking review finding is corrected;
- a fresh closure attestation conforms at the corrected commit; and
- a small closure record pins the reviewed commit and attestation hash.

The author correction pass and ordinary documentation gates are not independent review.

## Exact next work

**The next closure review is on hold by owner decision of 2026-08-17, and step 4 is not the live path
until it resumes.** The decision, its reasons, the work that has to land first, and the conditions that
end the hold are in the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md). In one
sentence: sixteen reviews have produced findings in every cycle, the only instrument here that finds a
real defect is a person reading prose, and the surface that reading covers grows every time a defect is
corrected — so the measuring instrument is being fixed before another cold context is spent.

Nothing else in this policy changes. The independence requirements, the closure standard, the required
scope and verdicts, and the retained records are all unchanged and apply unmodified to whichever review
runs next. **No agent dispatches a closure review while this paragraph stands.** An author-side
iteration pass over the plan's work is not a closure review and remains available, under the same rules
as every other iteration pass.

Twelve such passes have run and none has yet met the plan's condition 4, which asks for a pass that finds
nothing it can fix. They are retained as the
[first](./channel-0.2-am-iteration-review.md) (**AM1**-**AM3**),
[second](./channel-0.2-an-iteration-review.md) (**AN1**-**AN6**),
[third](./channel-0.2-ao-iteration-review.md) (**AO1**-**AO3**),
[fourth](./channel-0.2-ap-iteration-review.md) (**AP1**-**AP2**),
[fifth](./channel-0.2-aq-iteration-review.md) (**AQ1**-**AQ5**),
[sixth](./channel-0.2-ar-iteration-review.md) (**AR1**),
[seventh](./channel-0.2-as-iteration-review.md) (**AS1**-**AS7**),
[eighth](./channel-0.2-at-iteration-review.md) (**AT1**-**AT7**) and
[ninth](./channel-0.2-au-iteration-review.md) (**AU1**-**AU5**),
[tenth](./channel-0.2-av-iteration-review.md) (**AV1**-**AV3**),
[eleventh](./channel-0.2-aw-iteration-review.md) (**AW1**) and
[twelfth](./channel-0.2-ax-iteration-review.md) (**AX1**-**AX2**) W1-W3 verification-foundation
iteration reviews, each of which corrected everything it raised.

**A thirteenth pass over the same scope is the live path.** It starts by running the frozen instrument
set — `build/verify-channel-0.2-guards.ps1`, the coverage gate and the generated-vector run, whose
sizes the plan's section 4 owns and recomputes rather than this paragraph — and records what it
reports before building anything. That set now includes the generator, so it is strictly larger than
the eleventh pass's, which is what the 2026-09-04 ruling requires of the second of two consecutive
passes.

**Its method is the increment the eleventh pass left: teaching the generator to produce refusals.**
That generator evaluates all twenty-six properties over conforming vectors and is green over two
thousand of them, but it produces no refusal of any kind — no pre-dispatch refusal, no recorded
`unseen` refusal, no late-traffic latch, no declared stimulus steps. So `C4-P2`'s two conjuncts are
evaluated over empty observation records and are **vacuously green**, and the property eight finding
families have been about is the one that instrument reaches least.

Nothing in this paragraph resumes the closure cycle or authorizes a closure-review dispatch.

The sixteenth review has run, from a fresh isolated clone, and returned `does-not-conform` with
blocking **AL1** and **AL2** and nonblocking **AL3** and **AL4**; its retained record is
`channel-0.2-design-foundation-closure-review-16-attestation.md`. **Steps 1 through 3r are complete.**
Step 4 is the path the programme returns to when the hold ends, and the review that runs then reviews
the AL corrections together with the verification work done under the hold.

The fifteenth review returned `does-not-conform` with
blocking **AK1** and nonblocking **AK2**-**AK4**; its retained record is
`channel-0.2-design-foundation-closure-review-15-attestation.md`. **Steps 1 through 3q are complete.**

The fourteenth review returned `does-not-conform` with
blocking **AJ1** and nonblocking **AJ2**-**AJ7**; its retained record is
`channel-0.2-design-foundation-closure-review-14-attestation.md`. **Steps 1 through 3p are complete.**

The thirteenth review returned `does-not-conform` with
blocking **AI1** and nonblocking **AI2**-**AI9**; its retained record is
`channel-0.2-design-foundation-closure-review-13-attestation.md`. **Steps 1 through 3o are complete.**

The twelfth review returned
`conforms-with-nonblocking-findings` — the first non-negative verdict here — with six findings and no
blocking one. Its retained record is `channel-0.2-design-foundation-closure-review-12-attestation.md`.
Under the 2026-08-15 closure-standard ruling that verdict did not close the batch. **Steps 1 through
3n are complete.** Step 4 is the live path, and the next agent reviews the AH corrections.

**Two method notes the eleventh review left, both about how corrections fail here rather than about
the design.** First, four findings in a row were closed in the *first* artifact their evidence named
and left open in the second: AE4 to AF2, AE5 to AF3, AF1 to AG1, AF2 to AG4. A reviewer should take
each retained finding's evidence sentences, not its title, and re-derive each one. Second, and newer:
a correction asserted that another artifact carried a qualifier, and it did not. For every claim a
correction makes about an artifact it did not edit, open that artifact — both of the eleventh
review's independently reached findings came from that question, and neither was visible from the
diff.

The ninth review's retained
record is `channel-0.2-design-foundation-closure-review-9-attestation.md`, the eighth review's remains
`channel-0.2-design-foundation-closure-review-8-attestation.md`, and the seventh review's remains
`channel-0.2-design-foundation-closure-review-7-attestation.md`. It verified every retained
finding B1 through AD3 closed, found **U1 closed as to falsifiability and not as to soundness**, and
raised blocking **AE1** with nonblocking **AE2**-**AE5**. **Steps 1 through 3k are complete.** Step 4
is the live path. The next agent reviews the AE corrections; it does **not** begin schemas or
implementation, and it does not create `channel-0.2-design-foundation-closure-record.md` unless its
own verdict conforms.

**Read AE1 before anything else.** Nine cycles asked of each fix *can this fail*. AE1 is what the
mirrored question found: *can this fail when it should not*. `C4-P2`'s first conjunct was red on a
conforming realization whose request the transport lost — a required member of its own adversarial
group — and the loss vector was indistinguishable from the named mutation in every field the property
may read. A property that cannot fail and a property that cannot stay green are the same defect from
opposite ends, and only one end had ever been audited. The correction reads the recipient's subsequent
admission of the refused identity; **AE3** is the structural half that makes the class visible in
future, and eleven capabilities still owe a required-green set. The tenth reviewer should assume the
mirrored question has further answers in it, and should run each property over its *required vector
group* rather than over the cases the contract's narrative names — the difference between those two
is exactly what the AD and AE passes found separately.

1. ~~Obtain an owner ruling on U1.~~ **Not required, and this is the reason.** S1 and R1 were choices
   between defensible designs and each needed a ruling. U1 was not: `C4-P2` asserted that
   `C4-control-precedes-request` was the mutation it must go red on, and it stayed green on it. A
   property that cannot fail is a defect against C12's own rule that "every property must be able to
   fail against a named incorrect implementation", so the correction restores what the design already
   claimed rather than selecting between options.
2. ~~Add a failing check for **U1** before correcting it.~~ **Done.** The design verifier keys off the
   claim that *depends* on falsifiability — C4 asserting that `C4-control-precedes-request` is the
   mutation `C4-P2` must go red on — rather than off the property's own wording, so deleting that
   claim cannot make the check pass while leaving an untestable promise standing. It then requires
   `C4-P2` to be stated over the refusal a reordering produces, to carry both direction conjuncts
   restricted to one endpoint's own frames, not to give the mutation a contradictory "rejected as
   nonconforming evidence" expectation, and the per-capability property audit to register the pair. It
   failed with five findings before the correction and was mutation-tested afterwards by weakening
   each conjunct, restoring the contradictory sentence, reverting the audit row, and renaming
   `C4-P2` — each of which fires it again.
3r. ~~Correct AL1-AL4, raised by the sixteenth independent closure review, and check the recognizers
   against something outside themselves.~~ **Done.** **AL1** and **AL2** were both blocking and they
   are the same lesson from two directions: a guard that recognises a defect by the words the defect
   uses cannot see the instance that does not use them.

   **AL1** — `S3` bounded admission by "the first drain transition" and named no session. AH1 made a
   vector able to carry two, so a second session that legally establishes and admits after the first
   one drains violates the property as written, and it is red on behaviour conforming in both
   sessions. That is AE1's defect through the quantifier, which is what AK7 and AK8 corrected on five
   other properties one commit earlier — and the audit that produced those five reported `S1`-`S6`
   clean. It could not have done otherwise: its trigger set is C12's declared list of per-session
   facts, `S3` names none of them because it reads the session's state *through a transition of it*,
   and the session machine's status block recorded the clean result as though the audit had covered
   the question. All six properties now name the session they are about, and the check for them is
   **structural rather than lexical**: every property of the session state machine is a statement
   about one session — that is what the machine is — so every one of them must carry a session scope.
   That class is total over the artifact by construction, which a list of facts is not.

   **AL2** — the refused-frame reference is published in five surfaces and the AK1 correction reached
   four. The one it missed is the state/event grid's two recipient `unseen` cells, which still
   enumerated the pre-AK1 record — a provenance, a detailed reason, a bare frame kind, and nothing
   that identifies which frame or which session the refusal is about. Both
   halves of the check written for AK1 are keyed to the reference's own name — the registered surface
   was the whole recipient *route*, which the prose 35 lines below the cells satisfies, and the
   package-wide sweep triggers on the phrase `refused-frame reference`, which the cells never used.
   The cells are now registered as two surfaces of their own, separately from the prose, and the new
   sweep is keyed to **the record instead of the reference**: a passage naming the refusal's detailed
   reason alongside the two other fields that only a full statement of that record carries is
   enumerating it rather than discussing it, and must publish the whole reference. That sweep fires on
   exactly the two cells at the parent commit and on nothing else in the package — including on the
   first draft of this paragraph, which listed the three fields and was caught by the check it
   describes.

   **AL3** and **AL4** are corrected as their evidence describes. **AL3** is the one to carry forward:
   C12's declared fact list was derived from the four facts read by the five properties the AK pass
   had found red, which is a class inferred from its own members — AF6 one level up, inside the
   declaration written to end that shape. The list now carries the session's own state, and it is
   checked against the neutral brief's vector format rather than against itself, so a fact the vector
   distributes per session cannot stay outside the trigger set. **This is the ninth consecutive cycle
   in which a correction reached some of a fact's surfaces and not all of them**, and the eighth in
   which a check written to prevent that class passed the instance it was written for.

3q. ~~Correct AK1-AK4, raised by the fifteenth independent closure review, and stop sampling: enumerate
   `C4-P2` completely and audit every operand at once.~~ **Done.** **AK1** was blocking and is the
   fourth instance of one shape on one property — an operator qualifier whose operand the record it
   reads does not publish. W5 was precedence with no committing endpoint, AH1 was precedence with no
   session, AI1 and AJ1 were the settling-frame reference with no session, and AK1 is AF8's membership
   scope over a record that named neither the session nor the interaction identity. It is the first
   instance on `C4-P2`'s **first** conjunct, whose operand no cycle had opened in fifteen reviews. The
   record now carries the **refused-frame reference** across all six surfaces that publish what it
   contains; its five fields are stated by those surfaces and are deliberately not restated here.

   **The enumeration is the part worth carrying forward, and it produced four more findings.** Each of
   the four previous instances was found by sampling one operand, so this pass listed every fact
   `C4-P1` and `C4-P2` read, found every artifact and section publishing each, and checked sufficiency
   at the scope each clause claims. The result is a table in the completeness review — operand, scope
   claimed, publishing surfaces, sufficiency — that the next cycle can check rather than rediscover,
   and the verifier pins it. Three rows came back `insufficient`. **AK5** is the rest of AK1's own
   operand: the conjunct's literal subject is *the committing endpoint*, which the record did not name,
   and one endpoint may commit two controls naming one identity in one session, so without an arrival
   ordinal a control committed before the request binds to one committed after it and the property goes
   red on delivery that matched commit order. **AK6** is the second conjunct's **second** precedence
   operand — "that endpoint's own frame that made the interaction terminal" — which **no artifact
   published at all**; it was read off the terminal form, and a form identifies one frame only while an
   endpoint commits at most one frame of that form for one identity, which a duplicate terminal from a
   nonconformant peer is exactly the violation of and is a required-green member of that property's own
   group. Both are now frame references in the same five-field form, and **the check is written over
   the class** "a frame a property reads is published as a frame reference" rather than over any one
   reference, so a fourth reference is registered or fails the sweep.

   **AK7** and **AK8** are the same shape one level up, and they are the answer to a question the
   enumeration asked and no cycle had: AH1 settled that a vector **may carry more than one session**,
   and that decision reached the declared stimulus step, the settling-frame reference and the refusal
   record — but never the property *statements*. `C4-P1` forbade an identity being dispatched twice and
   bounded the number of nonterminal interactions with no session named; `C4-P2`'s preamble quantified
   "for each interaction identity" across the vector; `I5` bounded concurrency against "the established
   finite bound"; `C1-P1` required exactly one established profile per vector; and `C3-P1` said "the
   established profile" where a vector may carry two. Each is red on a conforming two-session vector.
   C12 now **declares** which facts a vector may hold more than one of, and the check derives its
   trigger set from that declaration rather than from the members that happened to be visible — which
   is AF6's correction applied to a rule instead of to a family.

   **AK2**-**AK4** are corrected as the disposition history records. **AK2** is worth one line: the
   Channel index's reviews row has omitted the `W` family since AE4's own correction, and the AE4 check
   could not ask for it because that check derived its class from `### <family><n> ` headings while the
   W review records W1-W6 in a table. The class is declared in the provenance table above, so the check
   now derives from the declaration and keeps the headings as a union.

   **What this pass did not do, stated plainly.** It did not establish that the enumeration is total.
   The rows were derived by reading the two properties clause by clause, and a fact a property reads
   without naming — which is exactly what AK6 was — is caught by reading and not by parsing. The check
   pins that the table exists, that every declared frame reference and every declared per-session fact
   has a row, that no row reads `insufficient`, and that every row names a resolvable surface. A later
   cycle that finds a row missing has found a finding, not a typo.
3p. ~~Correct AJ1-AJ7, raised by the fourteenth independent closure review, and execute the sweep as a
   search before editing anything.~~ **Done.** **AJ1** was blocking and is AI1 surviving the commit
   written to close it. The settling-frame reference is published in **five** places and the AI1
   correction reached three; the two it did not reach are the state/event grid, which the neutral brief
   declares itself subordinate to, and the responsibility matrix row that *owns* the observation record
   and whose own status block asserted that the fact it owns and the fact the parity profile compares
   are the same fact. The reviewer reproduced AI1's exact false green on `C4-outcome-precedes-ack` from
   both, with the field list as the only variable. The migration ledger's new-evidence inventory states
   the same reference a sixth time. All six now publish it in one form — kind, session, interaction
   identity, committing endpoint, arrival ordinal — and the check is written over the reference rather
   than over an artifact list: every registered surface must carry the identical list, any
   publication-shaped passage anywhere in the eleven artifacts must carry the whole list, and the count
   is exact. The AI1 check iterated two artifacts and asserted its own completeness with `Count -lt 3`,
   a bound set to the number of lists in its own scope, thirty lines below four AC1 checks that already
   enumerated the correct four artifacts for the same reference.

   **AJ2**-**AJ7** are corrected as the disposition history records. Two are worth carrying forward.
   **AJ2** is the eighth consecutive cycle of entry-point staleness and the first in which the
   disposition was recorded in an artifact the finding was never raised against — the plan claimed AI2
   while AI2's two narratives were untouched — so the narratives are rewritten and the check now derives
   from the declared provenance table that every numbered closure review is introduced by ordinal in
   each narrative with its family named there. **AJ5** is a check passing a row it was written to fail:
   `\bAI\b` does not match `AI9`, so four rows discharged a family obligation by naming one finding of
   it, and two of those four were false for AJ1's reason.

   **The sweep was executed as a search this time, before any artifact was edited.** `grep settl` over
   `docs/future/channel/` returns all six settling-frame surfaces in one screen; the previous pass
   changed the sweep axis to the concept and then computed the impact set from memory of the artifacts
   it had been editing, which reproduced AI1's own two-artifact evidence list and called it the concept.
   Each of the last three cycles' blocking findings — AG1, AI1, AJ1 — was one search away from the pass
   that missed it. A reviewer should treat the search as the minimum rather than as the answer: it finds
   surfaces that use the fact's vocabulary and would not find one that paraphrases it.
3o. ~~Correct AI1-AI9, raised by the thirteenth independent closure review, and change the sweep
   axis.~~ **Done.** **AI1** was blocking: AH1 declared multi-session vectors legal and gave the
   declared stimulus step a session so the precedence relation had its operand, and left the
   settling-frame reference — the other operand of the same property — published in three places as
   four fields with no session, still asserting it maps to one declared step. It stops mapping to one
   the moment two sessions may hold one identity value, and `C4-P2` then goes green on
   `C4-outcome-precedes-ack`. All three lists now carry the session. **AI9** is the one a reviewer
   should weigh hardest: S3's evidence named the plan's section 7.8, which reported seven retained
   negative attestations and stopped at the seventh review, so **a retained finding was open for six
   cycles while every index said the programme's findings were closed**. **AI2**-**AI8** are corrected
   as the disposition history records.

   **The sweep axis changed.** The AG sweep enumerated the artifacts each finding's *evidence cites*,
   and both AH2 and AI1 were unreachable by it — neither artifact was cited by the finding whose
   correction invalidated it. The axis is now the concept: when a correction changes a fact, the impact
   set is every artifact asserting something about that fact. AI4's check reads every artifact's status
   block and the settling-frame check reads every published field list, rather than the ones a finding
   named. A reviewer should treat that as a change of method with one round's evidence behind it, not
   as a solved problem — the previous method was also introduced with a rationale.
3n. ~~Correct AH1-AH6, raised by the twelfth independent closure review, and rule the closure
   standard.~~ **Done.** The twelfth review returned `conforms-with-nonblocking-findings` with no
   blocking finding. **The 2026-08-15 ruling recorded in the redesign plan settles that only an
   unqualified `conforms` closes the batch**, so that verdict stands as issued and did not close it;
   the alternative and its rejection are recorded with the ruling. **AH1** gave the declared stimulus
   step a session, which AG2's qualifier had no operand without — W5 inside the AG2 correction — and
   settled the question underneath both: a vector **may** carry more than one session. **AH2** is the
   fifth closed-in-the-first-artifact instance and the one the AG sweep structurally could not reach,
   since that sweep read the artifacts each finding's *evidence cites* and AF5's evidence never cited
   the audit. **AH3**, **AH4**, **AH5**, and **AH6** are corrected as the disposition history records.

   The next reviewer should know the twelfth review reached a conforming-with-findings verdict and
   that the bar was then set above it. That sequence is disclosed deliberately: the standard was ruled
   after a verdict arrived that the ruling excludes, which is the one shape this programme avoids, and
   the ruling says so and gives its reasons rather than leaving the timing unremarked.
3m. ~~Correct AG1-AG5, raised by the eleventh independent closure review, and sweep every retained
   finding's named artifacts.~~ **Done.** **AG1** was blocking and is the fourth instance of one shape:
   AF1's evidence named two artifacts and quoted both, the correction closed C4 and stopped, and the
   check written for it read the contract alone. The completeness review's silence-probe row still gave
   the mutation's expected observation as the recorded refusal, so a vector authored from it took
   `C4-P2` green on its own named mutation — U1 surviving inside the commit written to close it.
   **AG2** is a different and sharper class, and the one worth carrying forward: a correction asserted
   that another artifact carried AF8's session qualifier, and that artifact did not. The qualifier is
   now in the brief's operator set and the claim is pinned against it, so a sentence about another
   document cannot be written without that document agreeing. **AG3** records the AE1 ruling as issued
   and its AF8 narrowing, the treatment the S1 ruling already had. **AG4** and **AG5** are the third
   and fourth surfaces of the index staleness AE4 and AF2 each closed one of; every per-artifact row
   now states its position against the newest family or declares itself unchanged by it.

   **The sweep.** All eleven retained attestations were parsed for every finding's own evidence
   section: 47 findings cite artifacts and 23 cite more than one. Everything through AD had been
   verified individually by reviews 8-11 in the artifacts it was raised against, so the live set was
   AE, AF, and AG. A reviewer should treat that sweep as a starting point rather than a result — it is
   mechanical extraction by the correction author, and the class it exists to catch is one that author
   has now produced four times.
3l. ~~Correct AF1-AF8, raised by the tenth independent closure review.~~ **Done.** **AF1** was
   blocking and is the one worth reading: the AE1 correction made `C4-P2`'s first conjunct read the
   recipient's subsequent admission, and the passage stating what the mutation vectors' expected
   observations *are* still said they are "exactly" the recorded refusals. A vector authored from that
   passage leaves the membership test an empty set and takes the property green on its own named
   mutation. Two paragraphs of C4 contradicted each other, both gates stayed green, and the correction
   author's own summary of surfaces updated did not include it. The passage now states the complete
   record set both endpoints produce. **AF2**-**AF3** were the second halves of the AE4 and AE5
   corrections, in the same files, left behind by a pass that fixed the countable part. **AF4** put
   the admission into the new-evidence inventory. **AF5** named all seven legal members of the
   required-green set instead of four. **AF6** replaced the derived iteration class with the declared
   provenance table above. **AF7** brought `S1`-`S6` and `I1`-`I7` into the audit, where the record now
   shows `I1`-`I7` satisfy neither half of C12's rule. **AF8** scoped the membership operand to the
   session, since interaction identity is unique only within one.
3k. ~~Correct AE1-AE5 and AD2, raised by the ninth independent closure review.~~ **Done**, under the
   2026-08-14 AE1 owner ruling recorded in the redesign plan. **AE1** — the conjunct now also requires
   that the recipient afterwards admits an interaction for the refused identity, which a reordering
   produces and a loss does not; the parity profile compares that admission, and the contract names
   the lost-request vector among the inputs the property must leave green. Two rejected options are
   recorded with the ruling: scoping the conjunct to the declared injection, which would make a
   property read harness metadata, and dropping the conjunct, which would retire a promise to fit a
   property that could not express it. **AE2** — both `unseen` rows state effect certainty
   `known-none`, the value the totality rule would have supplied before X3 and Y3 moved that route out
   of it. **AE3** — C12 now requires a property not to fail against a conforming realization, the
   property format carries a required-green set as a normative field, and the per-capability audit
   carries the column; eleven of its twelve cells read `owed`, which is named residual work rather
   than a guessed set, and Batch 2 cannot author `capability-properties.json` until they are stated.
   **AE4** — the Channel index names every family the retained iteration reviews record, spelled out
   rather than compressed to a range, and the AD3 check now covers that third surface. **AE5** — the
   retained requirements register is in the migration ledger's sources inventory and `CH-R10` is
   dispositioned explicitly. **AD2** — the X7 class check derives its families from the policy's own
   iteration-pass attributions instead of testing two literals.
3j. ~~Correct AD1 and AD3, found by a ninth iteration pass over the retained records themselves.~~
   **Done**, and retained as the
   [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md). **AD1** — the AC
   review's residual stated that the AA and AB passes had left no retained record and referred the
   choice between reconstructing them and rescoping the requirement to the owner; the W iteration
   review has recorded AA1-AA3 and AB1-AB2 under its fourth and fifth pass headings all along. Acting
   on that residual would have produced duplicate records or rescoped a requirement nothing had
   violated. It was made by reading the W review's roster entry and scope line instead of the
   document — AC1 committed by the pass that raised AC1, one section below where it raised it.
   **AD3** — those two descriptions and the residual gave three different accounts of what the W
   review contains, none matching it; each is what some later pass consulted instead of opening it.
   Both are corrected, and the verifier now derives each retained review's families from its own
   finding headings, so a description cannot understate the document it describes without failing the
   gate. **AD2** is **open**: the X7 comment names two halves of its class check, AC4 widened the
   first, and the second is still written over two ids while the policy bolds thirty-six. There is no
   live gap — every family does have a retained record — so it is an owner call on whether the
   hardcoded pair is a defect or a deliberate narrowing the comment describes badly.
3i. ~~Correct AC1-AC4, found by an eighth iteration pass.~~ **Done**, and retained as the
   [AC correction iteration review](./channel-0.2-ac-correction-iteration-review.md). **AC1** — Y4's
   arrival ordinal was stated in the neutral brief and nowhere else, and the brief is subordinate to
   the contract, both state machines, and the grid, so the hierarchy resolved the contradiction
   against the fix: the interaction machine that owns the latch, the grid that enumerates the cells
   asserting it, and the matrix row AB2 had just added all still named X1's three fields. It was found
   by evaluator rather than by reading — under the machine's fields the duplicate terminal the
   property must leave green is undecidable. **AC2** — V1 made the peer-fault detailed reason
   normative wherever its category declares a closed set, and the ledger's set for
   `invalid-interaction-correlation` had five identity reasons, none of which covers an identity never
   opened; the value the first conjunct quantifies over was absent from the closed set that carries
   it, and both `unseen` cells recorded one provenance while the conjunct reads a cancellation control
   alone. **AC3** — both conjuncts said "no endpoint records … the same endpoint had already
   committed", whose nearest antecedent is the recording endpoint, which never commits the frame in
   question; the literal reading was unsatisfiable and therefore unfalsifiable, which is U1 through a
   pronoun. **AC4** — the check written over the X7 class matched one-letter finding families only, so
   it could not see AA, AB, or the findings of the review that retains it.
3h. ~~Correct AB1-AB2, found by a seventh iteration pass over the two artifacts the others never
   opened.~~ **Done.** **AB1** — the redesign plan is the fourth entry point and the one status block
   the T4 cycle-name check set never covered, and it had stopped at S3 while six passes ran. **AB2** —
   X5, Y1, and Y2 made the local observation record what `C4-P2` reads, and the responsibility matrix
   owned the observability system that consumes observations while the record itself had no owner row.
   That is S1's defect at S1's place in S1's artifact, six families after S1 was called closed, and it
   appeared because the passes that made the record load-bearing were the passes that could not see
   it.
3g. ~~Correct AA1-AA3, found by a sixth iteration pass that left the design package and read the
   entry points.~~ **Done.** **AA1** and **AA2** — the Channel index and the future-work index had
   fallen behind every finding family since V2, the second still naming S1 as the open blocking
   finding four families after it closed, and both understating the retained review count; the
   verifier now computes both counts from the reviews directory and requires every family in the
   disposition history to appear in both indexes. **AA3** — the future-work index still attributed
   the ordering row to `channel-core`, the identifier U2 abolished, so the closed owner vocabulary
   was closed in one artifact only; the identifier is now rejected in every Channel status entry
   point.
3f. ~~Correct Z1-Z4, found by a fifth iteration pass over the Y corrections.~~ **Done**, recorded in
   the same iteration review. **Z1** — the arrival ordinal Y4 added is observed arrival order, which
   W1 removed from the property language on purpose; it is restricted to identification and may never
   be an ordering operand. **Z2** — the grid's `unseen` cells still named `rejected-protocol` in the
   format every other row uses for a next state, which Y3 had just settled is a provenance. **Z3** —
   C10 gained the latch under Y1 and stopped at the terminal interaction's, leaving X2's
   `not-applicable` compared and unowned. **Z4** — the migration ledger's inventory of 0.2 cases with
   no 0.1 predecessor did not list intra-interaction frame order or its mutations, so the requirement
   every finding since S1 turns on was missing from the list of what Batch 2 must build. Z4 is the
   one finding in the sequence that no correction introduced; it is a hole all of them walked past.
3e. ~~Correct Y1-Y4, found by a fourth iteration pass over the X corrections.~~ **Done**, recorded in
   the same iteration review. Three of the four are one question — X1 and X5 made `C4-P2` read facts
   and nothing had been asked to carry them. **Y1** — C10's enumeration and the brief's
   local-observation schema named neither the latch nor its settling frame, so the parity profile
   compared two fields no observation holds. **Y2** — C10 requires an observation for every attempted
   establishment and interaction, and the `unseen` refusal is neither; the record X5 depends on was
   owned by nothing. **Y3** — X3 routed the refusal to `rejected-protocol`, a terminal state, which
   the `any terminal` rows claim and latch; the recipient's per-identity state remains `unseen` and
   `rejected-protocol` is the provenance, which also makes W4's later-request sentence a consequence
   rather than an assertion. **Y4** — the settling-frame reference could not separate two frames of
   the same kind from one endpoint, which is what a duplicate terminal is and which must leave the
   property green, so it carries the frame's arrival ordinal.
3d. ~~Correct X1-X7, found by a third iteration pass over the W corrections.~~ **Done**, and retained
   as the [W correction iteration review](./channel-0.2-w-correction-iteration-review.md). Each was
   found by asking what a W fix *depends on*. **X1** — W6 made the late-traffic latch value
   comparable, and `C4-P2`'s second conjunct reads the frame the latch settled *against*; the mutation
   and the two cases the property must leave green all record `state-violation` with
   `fault-committed`, so the settling frame is now recorded where the latch settles and compared in
   the parity profile. **X2** — W4 created a route with no latch while the grid requires every
   generated cell to assert one, so the absence is an explicit `not-applicable` value. **X3** — the
   recipient transition table had no row for a control at `unseen`, so the machine's own totality rule
   produced the terminal `peer-fault` with a latch that W4 refuses; the row now exists. **X4** —
   `C4-outcome-precedes-ack` was in no required adversarial vector group. **X5** — the first conjunct
   quantifies over a record W4 said the recipient does not keep; recording evidence is now
   distinguished from retaining state, and the distinction is what makes both W4 and the property
   true at once. **X6** — the pin clause that closed U6 went stale one commit later, and is now
   checked against the repository rather than against its own wording. **X7** — the W passes left no
   retained iteration review and V3's disposition was unrecorded.
3c. ~~Correct W5 and W6.~~ **Done.** Both were found by asking what the W1 fix *reads* rather than
   whether it is worded correctly. **W5** — the precedence relation is defined over one endpoint's own
   frames, and the vector format recorded "ordered stimulus steps" with no committing endpoint, so the
   operator had no operand; steps now name theirs. **W6** — the state/event grid requires every
   generated cell to assert the late-traffic latch and the normative parity profile never compared it,
   so `C4-P2`'s second conjunct read a fact that was demanded as evidence and excluded from comparison
   at once; the parity profile now carries it.
3b. ~~Correct W1-W4, found by a second iteration pass over the U1/U2-U8 corrections.~~ **Done.** Each
   is the U1 family one layer further down, and each was found by asking a different question than
   "is this worded correctly". **W1** — `C4-P2` turns on "had already committed" and "committed
   before", and the closed property operator set had no ordering relation at all, so the property was
   not *writable* in the form the brief requires of every property. A bounded precedence relation over
   one endpoint's declared stimulus steps is added, deliberately the narrowest one that makes `C4-P2`
   expressible and explicitly not comparable across endpoints or against anything observed. **W2** —
   nothing said what the reordering provider declares at establishment; it declares per-interaction
   frame order and then violates it, and establishment verifies the declaration is present rather than
   true, which is precisely why the S1 correction needed both a declaration and a property. **W3** —
   `C4-P2` had two conjuncts and one named mutation, so the recipient-to-initiator conjunct was
   unfalsifiable by name; `C4-outcome-precedes-ack` is added. **W4** — nothing said whether a
   recipient retains a terminal history for an identity refused at `unseen`. It retains none: the
   identity never entered the replay set, and a retained record would be the unbounded state the R1
   ruling refused. That rule now appears in C4, the interaction machine, and the grid, because the
   latch otherwise claims every terminal interaction.
3a. ~~Correct the nonblocking findings U2, U3, U4, U7, and U8.~~ **Done**, each with a failing check
   written first and mutation-tested after. The responsibility matrix now declares a closed
   owner-identifier vocabulary and the ordering row is owned by `channel`, not a second name for the
   same family (U2). The neutral brief's establishment rule carries the realization's per-interaction
   frame order declaration and the required adversarial groups include one owning the ordering
   mutation (U3). The completeness review's disposition history runs to the eighth cycle instead of
   stopping at the fifth (U4), and its in-flight direction-scope row records that `C4-P1` and `I5`
   read session-wide while the reservation mechanism can enforce only per-direction, rather than
   calling the scope undeclared (U7). The initiator grid's pre-dispatch Local loss cell names `lost`
   like every other cell in that column (U8). One of these checks was itself found weak by mutation
   testing — a phrase-anywhere test that the artifact's own status block satisfied — and was scoped to
   the section that has to carry the rule.
3. ~~Correct U1 contract-first.~~ **Done.** `C4-P2` is restated over the refusal reordering produces
   rather than over the accepted sequence: no endpoint records a recipient `rejected-protocol` at
   `unseen` for a cancellation control whose request the same endpoint had already committed, and none
   records a late-traffic `state-violation` latched against a frame the same endpoint committed before
   the frame that made the interaction terminal. Restricting each conjunct to one endpoint's own
   frames is load-bearing, and was found by probe rather than by reading: without it a legal late
   control after a peer's terminal, and a duplicate terminal from a nonconformant peer, both fail the
   property. The mutation vector's expected observation is now the recipient's recorded refusal, which
   is a determinate portable observation under C12-P1, rather than the vector being rejected before it
   executes. The per-capability property audit registers `C4-P2` and its mutation.
4. **On hold since 2026-08-17 — do not dispatch.** The state is declared in the
   [verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md) and the
   design verifier reads it from there: while the hold stands, retaining a seventeenth attestation
   fails the gate. Obtain another fresh independent review of the
   corrected pin, from a reviewer identity distinct from the correction author and all sixteen
   retained reviewers, **in a fresh isolated clone**. Its
   scope, verdicts, and probe requirements are unchanged from the sections above. It writes only its
   own attestation. The hold and the four conditions that end it are in the
   [verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md); this
   step is unchanged in substance and resumes exactly as written when they are met.

   The reviewer should treat the U1 correction as the primary target, and should treat the disclosed
   process deviation below as a reason to weigh it harder rather than less. The lesson four cycles
   have now paid for is that **a correction is not finished when the fact has an owner; it is finished
   when a property can refute it** — so the reviewer should not read `C4-P2` and agree with it, but
   write an evaluator from the published prose and run the mutation through it, as the eighth review
   did. The sharpest questions: does the refusal-based formulation admit a reordering that produces
   neither named fault; does restricting both conjuncts to one endpoint's own frames leave any
   reordering the promise forbids unwitnessed; can the recipient's `rejected-protocol` at `unseen` be
   distinguished in the observation record from the other causes of that same terminal, given C10
   carries no frame-order field; and do **U2**-**U8**, all still open, interact with the correction.

   Questions the X and Y passes raised and did not settle. The observation record is now load-bearing
   for `C4-P2` and no artifact bounds it: X5 argues that recording is not retaining because nothing
   consults the record, which is a claim about what reads it rather than about how much of it a peer
   can cause, and Y1/Y2 then made two more capabilities depend on the record existing. Does a frame
   refused before correlation consume an arrival ordinal? And is `not-applicable` the right
   disposition for a latch on a route that reaches no terminal interaction, or does that route belong
   outside the latch's column entirely — a second such route would today rest on the grid's prose to
   be recognised as needing the value at all.
5. If that verdict conforms, retain and commit the attestation unchanged, calculate its SHA-256, then
   create `channel-0.2-design-foundation-closure-record.md`. The record contains the reviewed commit,
   attestation path and hash, reviewer identity/date/verdict, all four owner rulings, confirmation
   that every retained finding closed with no new blocker, and the exact validation results. Update
   this README, the Channel index, the redesign plan, `docs/future/README.md`, and the design verifier
   so they accept exactly the conforming attestation and closure record and say Batch 2 is open.
6. Run, in order:

   - `build/verify-channel-0.2-design.ps1`;
   - `build/verify-channel-0.2-design.ps1 -NegativeProbe` and confirm it fails only because
     `C12-P1` was removed in memory;
   - `build/verify-channel-0.2-properties.ps1`, which evaluates `C4-P2` over its eleven declared
     inputs and reverts nine published operands, failing when a verdict is not the declared one.
     A reviewer building an evaluator of their own should compare against it and treat a
     disagreement as a finding against whichever of the two is wrong -- the declared expectations
     are data in `conformance/channel-0.2-properties.json` and are as reviewable as the prose;
   - `build/verify-channel-0.2-facts.ps1`, which renders every owned fact into the artifacts that
     publish it and fails on a hand-edited or unfenced publication. A reviewer checking whether the
     twenty frame-reference publications agree no longer has to compare them: they are generated
     from one declaration, and the reviewable question is whether that declaration is right;
   - `build/verify-doc-links.ps1`;
   - `build/verify-text.ps1`; and
   - `build/verify-interchange.ps1`.

Only after the conforming attestation, closure record, documentation/status updates, and clean full
gate are committed may the next agent start Batch 2 from the
[neutral contract brief](../Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md). This closure
authorizes planned schema work; it does not ratify Channel 0.2 or claim implementation conformance.

## Retained records other than attestations

[`channel-0.2-disposition-index.md`](./channel-0.2-disposition-index.md) owns the per-artifact
correction history that each design artifact's status block used to carry, moved there verbatim
under **W3** of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md). It is
not an attestation, is assessed by no review, and never overrides a retained record: where it and an
attestation disagree the attestation is right. A reviewer checking what was corrected in one
artifact reads it; a reviewer checking what a finding *was* reads the attestation that raised it.

## Retained attestations

- [Original design-foundation review](./channel-0.2-design-foundation-attestation.md) — reviewed
  `66729b097b032febf498dd907dd2387e2aebc2c5`; `does-not-conform`; B1-B4 retained for closure
  comparison.
- [First closure review](./channel-0.2-design-foundation-closure-attestation.md) — reviewed
  `e863bf15fca30466d6e262b0ea66b3c05bc384eb`; `does-not-conform`; B1-B4 closed and N1-N3 retained
  for final closure comparison.
- [Final closure review](./channel-0.2-design-foundation-final-closure-attestation.md) — reviewed
  `1af7ba0018c874750e346ee687f07ea1d302adef`; `does-not-conform`; B1-B4/N1-N3 closed and F1-F3
  retained for definitive closure comparison.
- [Definitive closure review](./channel-0.2-design-foundation-definitive-closure-attestation.md) —
  reviewed `1b7c5fdea0dc555a64152eea055fcebad053cf90`; `does-not-conform`; all earlier findings closed and
  D1-D5 retained for totality closure comparison.
- [Totality closure review](./channel-0.2-design-foundation-totality-closure-attestation.md) —
  reviewed `5cf42c4d97083324ffb8d6bd68491a145b8e611a`; `does-not-conform`; D1-D5 closed, blocking T1
  and nonblocking T2-T4 retained for closure re-review comparison.
- [Closure re-review](./channel-0.2-design-foundation-closure-re-review-attestation.md) — reviewed
  `11ba93bddbd38f03df59b4afc5166d7c6991c865`; `does-not-conform`; T1-T4 closed, blocking R1 and
  nonblocking R2-R3 retained for the next closure comparison. **Its isolation is partial and the
  attestation says so**: no fresh isolated clone was used, and the reviewing session had already read
  the future index and this policy while identifying the work. Under the owner ruling recorded in
  [`docs/future/README.md`](../../README.md#channel-02-first-batch-remaining-work), navigational
  reading of the indexes to locate the work does not by itself spend context freshness; the absent
  isolated clone still does. It therefore establishes R1 but could not have closed the batch.
- [Closure review 7](./channel-0.2-design-foundation-closure-review-7-attestation.md) — reviewed
  `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`; `does-not-conform`; T1-T4, R2, and R3 closed, R1 closed
  at `validating` but **not** at `unseen`, blocking S1 and nonblocking S2-S3 retained for the next
  closure comparison. **Its isolation is complete**: a fresh isolated clone was used, the reviewer
  identity differs from all six earlier reviewers and from the correction author, and no author
  private reasoning was available. The attestation records what its dispatching brief did name — the
  primary target and three specific silence checks — and states that S1 was not among them and was
  reached independently.

- [Closure review 8](./channel-0.2-design-foundation-closure-review-8-attestation.md) — reviewed
  `3b27e3a85bf018bead6d226a13d075c7e6ed16fa`; `does-not-conform`; every retained finding through
  S1-S3 verified closed individually, S1 closed as to ownership but **not** as to falsifiability,
  blocking **U1** and nonblocking **U2**-**U8** retained for the next closure comparison. **Its
  isolation is complete**: a fresh isolated clone at a short path, 881 tracked paths materialised,
  reviewer identity distinct from all seven earlier reviewers and from every correction author, and
  no author private reasoning available. It reproduced the grid enumeration independently (108
  published-row cells, 180 underlying state/event pairs) and reached U1 by writing a property
  evaluator from the published prose and running the property's own named mutation through it. The
  attestation records that U1 answers a question its dispatching brief named, and that U2-U8 did not.

- [Closure review 9](./channel-0.2-design-foundation-closure-review-9-attestation.md) — reviewed
  `940894839e2abe9a3e54536b2ed24c1f18bf6598`; `does-not-conform`; every retained finding B1 through
  AD3 verified closed, **U1 closed as to falsifiability and not as to soundness**, blocking **AE1**
  with nonblocking **AE2**-**AE5**, and a recorded verdict that the open **AD2** call is a defect.
  **Its isolation is complete**: a fresh isolated clone at a short path, 886 tracked paths, reviewer
  identity distinct from all eight earlier reviewers and from every correction author, nothing read
  from the author's working repository. It reached AE1 by writing a `C4-P2` evaluator from the
  published prose and running the *required adversarial group* through it rather than the cases C4's
  narrative names — which is the step that separates it from the AD pass, whose own evaluator found
  nothing because it ran the named cases only. Its dispatch is disclosed below.

- [Closure review 10](./channel-0.2-design-foundation-closure-review-10-attestation.md) — reviewed
  `c358464263a1131f91bc4e96b3dcc353d1fcd5b7`; `does-not-conform`; blocking **AF1** with nonblocking
  **AF2**-**AF8**. **Its isolation is complete**: a fresh isolated clone at a short path, 887 tracked
  paths, reviewer identity distinct from all nine earlier reviewers and every correction author. It
  verified the pin independently — `git diff --stat` between the named commit and the reviewed merge
  is empty — reproduced the 108-cell/180-pair grid enumeration agreeing with reviews 8 and 9, and
  **confirmed the AE1 correction works** before finding it incomplete one artifact below itself. It
  also recorded two probes that found nothing, which is worth as much as the ones that did. Its
  dispatch is disclosed below, and is the strongest disclosure in this directory: the session that
  dispatched it had authored the very commit under review.

- [Closure review 11](./channel-0.2-design-foundation-closure-review-11-attestation.md) — reviewed
  `57bb1d85292e5a0cf948f98c146131107cff1634`; `does-not-conform`; blocking **AG1** with nonblocking
  **AG2**-**AG5**, and AF3-AF7 closed completely. **Its isolation is complete**: a fresh isolated clone
  at a short path, 888 tracked paths, reviewer identity distinct from all ten earlier reviewers and
  every correction author. It verified the pin by tree hash rather than by file list, enumerated the
  grid independently to 108 cells and 180 pairs — agreeing with reviews 8, 9, and 10 — recomputed all
  eleven registry SHA-256 pins, and recorded two probes that found nothing. It is also candid that
  **AG1 was already documented by its predecessor**: review 10's AF1 named the artifact and quoted it,
  so this review proved the finding still live rather than discovering it, and says so rather than
  claiming the credit. Its dispatch is disclosed below.

- [Closure review 12](./channel-0.2-design-foundation-closure-review-12-attestation.md) — reviewed
  `f451f557ec51b9b878ddc0476c1cc7e0bd836679`; **`conforms-with-nonblocking-findings`**, the first
  non-negative verdict in the programme; **AH1**-**AH6** and no blocking finding. **Its isolation is
  complete**: a fresh isolated clone, 889 tracked paths, reviewer identity distinct from all eleven
  earlier reviewers and every correction author. It verified AG1 closed by evaluator rather than by
  reading — the same vector authored from the corrected row goes red where it went green at `57bb1d8`
  — enumerated the grid to 108 cells and 180 pairs agreeing with reviews 7-11, recomputed all eleven
  registry pins, and recorded two probes that found nothing. **It did not close the batch**: under the
  2026-08-15 closure-standard ruling only an unqualified `conforms` does. It also flagged its own
  closest call rather than presenting it as settled — that **AH2 is structurally what AG1 was**, and
  that if the owner judged AF5 under-rated then AH2 is blocking and its verdict wrong. That flag is
  why the ruling was made. Its dispatch is disclosed below.
- [Closure review 13](./channel-0.2-design-foundation-closure-review-13-attestation.md) — reviewed
  `e7bfeba6ba58e2e4e9a48a5148e2461c187bf452`; `does-not-conform`; blocking **AI1** with nonblocking
  **AI2**-**AI9**. **Its isolation is complete**: a fresh isolated clone, 890 tracked paths, reviewer
  identity distinct from all twelve earlier reviewers and every correction author. It recomputed all
  twelve registry pins, enumerated the grid to 108 cells and 180 pairs agreeing with reviews 7-12, and
  recorded three probes that found nothing. **AI1** is the AH1 decision propagated to one operand of
  two, and **AI9** is a retained finding — S3's own evidence surface — that had stayed open for six
  cycles while every entry point reported the programme's findings closed. Its dispatch is disclosed
  below.

- [Closure review 14](./channel-0.2-design-foundation-closure-review-14-attestation.md) — reviewed
  `6cddb990f1f8aada3018a19c63b43116b83f05e6`; `does-not-conform`; blocking **AJ1** with nonblocking
  **AJ2**-**AJ7**. **Its isolation is complete**: a fresh isolated clone, 891 tracked paths, clean
  status, reviewer identity distinct from all thirteen earlier reviewers and every correction author,
  and the author's working repository neither read nor executed against. It verified the pin in the
  tree-hash form and as to date, enumerated the grid to 108 cells and 180 pairs agreeing with reviews
  7-13, and recorded three falsification attempts that found nothing alongside the one that did.
  **AJ1** is AI1 surviving the commit written to close it: the settling-frame reference is published
  in five places, the correction reached three, and the reviewer reproduced AI1's exact false green on
  `C4-outcome-precedes-ack` from the two it did not — the state/event grid the neutral brief declares
  itself subordinate to, and the responsibility matrix row that owns the observation record. It also
  ran the AI1 check's own regex over the four candidate artifacts and recorded that it matches none of
  the lists in those two. **AJ2**, **AJ3**, and **AJ4** are re-derivations of AI2's, AI7's, and AI4's
  own evidence sentences, which it credits to its dispatching brief rather than claiming as
  discoveries. Its dispatch is disclosed below.

- [Closure review 15](./channel-0.2-design-foundation-closure-review-15-attestation.md) — reviewed
  `5cfa5ed71836082f0fb97e1be1873e49acde759d`; `does-not-conform`; blocking **AK1** with nonblocking
  **AK2**-**AK4**. **Its isolation is complete**: a fresh isolated clone, 892 tracked paths, clean
  status, reviewer identity distinct from all fourteen retained reviewers and every correction author,
  and the author's working repository neither read nor executed against. It verified the pin in the
  tree-hash form and as to date, enumerated the grid to 108 cells and 180 pairs agreeing with reviews
  7-14, recomputed all twelve registry pins, and recorded three falsification attempts that found
  nothing alongside the one that did. **AK1** is the fourth instance of the operator-with-no-operand
  shape on `C4-P2` and the first on its *first* conjunct: the recorded `unseen` refusal is that
  conjunct's operand, five surfaces publish what it contains, they agree with each other exactly, and
  none names the session AF8 scoped the membership test to. Its probe builds the two-session vector
  AF8's own text names as the failure it exists to prevent and takes the property red on behaviour
  conforming at both endpoints in both sessions. It is also the **first review in eight** to record
  that no finding was closed in the first artifact its evidence named and left open in the second, and
  it says so as a positive result of the change of correction actor rather than as an absence. Its
  dispatch is disclosed below.

- [Closure review 16](./channel-0.2-design-foundation-closure-review-16-attestation.md) — reviewed
  `95c62c104ba191e52f651c161c63407513238a73`; `does-not-conform`; blocking **AL1** and **AL2** with
  nonblocking **AL3** and **AL4**. **Its isolation is complete**: a fresh isolated clone, 893 tracked
  paths, clean status, reviewer identity distinct from all fifteen retained reviewers and every
  correction author, and the author's working repository neither read nor executed against. It
  verified the pin in the tree-hash form and as to date, enumerated the grid to 108 cells and 180
  pairs agreeing with reviews 7-15, recomputed all twelve registry pins, and recorded four
  falsification attempts that found nothing alongside the two that did. **It is the first review to
  carry a `C4-P2` evaluator that goes 11 for 11** — red on both named mutations and green on all seven
  required-green members plus the AK1 and AK5 vectors — so the property this programme has been about
  for eight families is sound at that pin, and it says so before raising anything. **AL1** is the
  quantifier defect AK7 corrected, in the artifact the AK audit reported clean, on the one property
  whose per-session fact is the machine's subject rather than a fact it names. **AL2** is AK1's own
  five-surface evidence list corrected in four places. It also audited the AK operand enumeration row
  by row and found no row missing and none wrong, and recorded that `AK6` moves no verdict on its own
  in any member of its property's group — a recorded non-finding rather than a raised one, on the
  ground that over-precision in an operand is not a defect. Its dispatch is disclosed below.

The current review target is the commit titled `verification: pin the eleven property obligations no
input reached`, committed 2026-09-03, which is the head of the correction sequence beginning at
`fix(channel): make C4-P2 falsifiable`. It moves the pin off `ci: put the gate self-checks behind an
explicit switch` because the ninth pass's **AU1** corrections reach a design artifact -- the
completeness review's per-capability audit rows for `C2`, `C3`, `C5`, `C7`, `C8`, `C9`, `C10`, `C11`
and `I6`, which now name a mutation for each obligation rather than for each clause. The pin was
previously moved off `feat(channel): retain the coverage instrument and close AR1` for the eighth
pass's **AT1**-**AT3** corrections, which reached the same audit for `I4`, `C6` and `C10`. The other commits above
the previous pin are verification-foundation work done under the hold rather than corrections to a
finding: all twenty-six properties now execute in
the gate with their required-green sets stated, the three frame references and the recipient `unseen`
refusal record are rendered from one declaration into the twenty-one artifact sites that publish them,
and the status blocks and index rows
carry a pointer to the
[disposition index](./channel-0.2-disposition-index.md) instead of their correction history. A
reviewer assessing this pin reads that index for what was corrected in an artifact and the retained
attestation for what the finding was. Review that commit or any later commit whose design
artifacts hash identically to it — and check that claim rather than assuming it, because this clause
has now gone stale three times: the eighth review raised it as **U6**, the rewrite that closed U6 was
itself superseded one commit later and raised as **X6**, and the same sentence carried the wrong date
for two cycles as **AI8**. The design verifier resolves this sentence's subject to a commit and
compares each design artifact's blob hash there against the tree a reviewer reads now, so a correction
pass that forgets it fails the gate rather than misdirecting a reviewer. It asks blob identity rather
than "which commit last changed a design artifact" because **AM5** established that the second
question has two different answers -- one on the merge commit a pull-request build checks out and one
on the linear branch -- and only the first is what `main` reports after the merge. This paragraph
described that superseded question for one commit after the check stopped asking it, which is **AN6**;
the artifact set it compares is the nine the required review scope names, which is **AN2**. The preceding pins
`3892c23a8dd4c7f298e877ba73710ee0ddc97bc4` and `3b27e3a85bf018bead6d226a13d075c7e6ed16fa` are what the
seventh and eighth reviews assessed and are nonconforming.

No conforming closure attestation exists yet. The corrected artifacts remain nonconforming evidence
until a fresh reviewer closes every retained finding and reports no new blocker.

## Retained iteration reviews

These are author-side passes, not attestations. None of them can close the batch, and each says so.
They are retained so a fresh reviewer can see what has already been examined and spend its cold
context elsewhere — never as evidence that their conclusions are right.

- [U1 correction iteration review](./channel-0.2-u1-correction-iteration-review.md) — the U1
  correction; raised V1 and V2, corrected both, and recorded V3 as an owner call.
- [W correction iteration review](./channel-0.2-w-correction-iteration-review.md) — the W1-W6
  corrections; raised X1-X7 and corrected all seven, then turned the same method on its own
  corrections four times more and raised Y1-Y4, Z1-Z4, AA1-AA3, and AB1-AB2, each recorded under its
  own pass heading. It is also the retained record for the two W passes, which left none of their
  own — that gap is X7. It is the longest of the three and the one a reader is most likely to
  under-read: the AA and AB families are in its fourth and fifth passes, not in a separate document.
- [AC correction iteration review](./channel-0.2-ac-correction-iteration-review.md) — the whole
  sequence through AB2; raised AC1-AC4 and corrected all four. AC1 was found by writing a `C4-P2`
  evaluator from the published prose, as the handoff below asks the closure reviewer to do, and
  running it twice: once with the settling-frame fields the neutral brief states and once with the
  fields the interaction machine states.
- [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md) — the sequence at
  `4a52a56`; raised AD1-AD3, corrected AD1 and AD3, and left AD2 as an owner call. It is the first
  pass to audit the retained records against the artifacts rather than the artifacts against their
  records, and all three findings came from that one question.
- [W1-W3 verification-foundation iteration review](./channel-0.2-am-iteration-review.md) — the
  verification work done under the hold, at `88f2447`; raised AM1-AM3 and corrected all three. It is
  the pass condition 4 of the verification foundation plan names and **does not meet that condition**,
  which requires a pass that finds nothing it can fix. It reviewed no design artifact: its method was
  to recompute every number the plan states, re-derive every claim one document makes about another,
  and break every guard W1, W2 and W3 added. AM1 came out of the third — the status-block length bound
  was measuring a paragraph the history it excludes had sat one blank line beneath.
- [Second W1-W3 verification-foundation iteration review](./channel-0.2-an-iteration-review.md) — the
  same scope at `0e43a69`, after the AM corrections; raised AN1-AN6 and corrected all six, so
  condition 4 is **still not met**. Its first act was to re-run every probe the AM review records, as
  that review's closing paragraph asks; all of them reproduced and none of the six findings came from
  them. Every one came from asking where else a corrected fact is stated: AN1 and AN2 are guards whose
  scope is narrower than the question their own comments claim, and AN3-AN6 are four facts corrected
  in the record that owns them and left standing in one to four other records.
- [Third W1-W3 verification-foundation iteration review](./channel-0.2-ao-iteration-review.md) — the
  same scope at `d01e706`; raised **AO1**-**AO3** and corrected all three, so condition 4 is **still
  not met**. It took the brief the AN review left — read each guard's comment as a claim and test it
  against the code — and the fifteenth such block gave **AO1**, which is the most serious finding the
  verification work has produced: `S1` and `C2-P1` were red on a conforming session fault, because the
  cross-check that exists to prevent exactly that could not read the transition table's two
  `any nonterminal` rows. **AO3** is the probes themselves: three passes rebuilt them from the prose of
  the pass before, four had rotted unnoticed, and they are now a corpus the gate runs.
- [Fourth W1-W3 verification-foundation iteration review](./channel-0.2-ap-iteration-review.md) — the
  same scope at `108f0c9`; raised **AP1**-**AP2** and corrected both, so condition 4 is **still not
  met**, on a falling count of three, six, three, two. It is the first pass to **run** the probes
  instead of rebuilding them — 45 of 45 in one command — which is what AO3 was for. Both findings are
  guards whose key was correct when written: **AP1** is a block of twenty-four checks that one deleted
  sentence silenced, on a justification W2 had quietly expired, and **AP2** is a coverage check
  sampling four of the twenty-six properties its own comment says it covers.
- [Fifth W1-W3 verification-foundation iteration review](./channel-0.2-aq-iteration-review.md) — the
  same scope at `138af11`; raised **AQ1**-**AQ5** and corrected all five, so condition 4 is **still
  not met**, and the count has stopped falling: three, six, three, two, five. It ran the corpus first
  — 53 of 53 — and then **built an instrument** rather than reading the gates a fifth time: each gate
  under a line trace, every statement that never executed reported. Four of the five findings are
  checks that no longer run at all, and **AQ1** is the sharpest thing this work has found — the AJ2
  narrative freshness check, the guard against the staleness that ran for eight consecutive cycles,
  has been an empty loop since the 2026-08-20 ruling inserted a column into the table it reads.
  **AQ5** is the class the trace cannot see: a negative assertion bounded by a character count, which
  under-reaches in silence as the passage it spans grows.
- [Sixth W1-W3 verification-foundation iteration review](./channel-0.2-ar-iteration-review.md) — the
  same scope at `a5ec7a5`; raised **AR1** and corrected it, so condition 4 is **still not met**. It
  **retained the instrument** the AQ pass discarded: `build/verify-channel-0.2-coverage.ps1` requires
  every conditional in a covered gate to be evaluated by a passing run. It ran in the repository gate
  until **AT7** moved it behind `build/verify-gate-self-checks.ps1`. **AR1** is what that found on its first run, and it is the first finding here raised by an
  instrument rather than by a reading — `C5-P1` and `C6-P1` each state two clauses, each had one named
  mutation, each mutation fires through the first clause, and both second clauses could be deleted
  from the evaluator with both gates green. It is also the first of the AM-AR passes raised against
  the **design**: the correction reached the per-capability property audit.
- [Seventh W1-W3 verification-foundation iteration review](./channel-0.2-as-iteration-review.md) — the
  same scope at `7bf34a1`; raised **AS1**-**AS7** and corrected all seven, so condition 4 is **still not
  met**. It audited what conditional coverage cannot see: negative assertions whose bodies run while
  their character-bounded subject under-reaches, and a compound recognizer whose fact-word operand
  cannot fire after ordinary prose growth. Five red probes are retained, and the clean package itself
  pins the sixth correction; a harness self-check pins the seventh correction. AS is a verification
  family and changes no first-batch design artifact.
- [Eighth W1-W3 verification-foundation iteration review](./channel-0.2-at-iteration-review.md) — the
  same scope at `ef4b94d`; raised **AT1**-**AT7** and corrected all seven, so condition 4 is **still not
  met**. It answered the two surfaces AS left it with an instrument rather than a reading: both were measured,
  and the coverage gate keeps a second unit — an operand of an
  `-and`/`-or` expression that no input reaches while the expression around it is evaluated. **AT1** is
  **AR1 on a property AR1's own correction could not reach**, because that correction keys on
  properties which declare a conjunct and `I4` declares none. Like AR, AT is a **design** family: the
  corrections reach the per-capability property audit.
- [Ninth W1-W3 verification-foundation iteration review](./channel-0.2-au-iteration-review.md) — the
  same scope, after the AT corrections; raised **AU1**-**AU5** and corrected all five, so condition 4 is
  **still not met**. It answered the brief AT left by rejecting its unit: the inherited class was an
  operand that runs and always takes the same value, 124 of 247, and separating those from defensive
  null checks is unanswerable because the two are the same shape. Measuring the verdict constructor
  instead reports only semantic obligations, and **AU1** is eleven of them across nine properties, each
  running on every declared input, never firing, and deletable outright with every gate green — AR1 and
  AT1 a third time, including inside the very clause AR1 was raised against. **AU2** is the same eleven
  audited for what would make them false pins: six properties could not tell a violation from a vector
  that omits the field, which is AE1 latent. Like AR and AT, AU is a **design** family: the corrections
  reach the per-capability property audit.
- [Tenth W1-W3 verification-foundation iteration review](./channel-0.2-av-iteration-review.md) — the
  same scope at `7798db4`; raised **AV1**-**AV3** and corrected all three, so condition 4 is **still not
  met**. It answered the brief AU left by finding the chokepoint those gates were said to lack —
  `$failures.Add` — and the inputs that reach it, which are the probes; the corpus makes 62 of 299
  guard sites fire. **AV1** is what building that measure exposed: a probe asserted the gate's exit
  code, so it could not tell its own guard firing from the gate failing for any other reason, and the
  corpus reported 77 of 77 while three gates were failing to parse. **AV2** is what the corrected check
  found on its first run — a guard that could never fire, because a failing git call terminates the
  gate above it under the gate's own `Stop` preference, with every check after that point skipped.
  Unlike AR, AT and AU, AV is a **verification** family: neither correction reaches a design artifact.
- [Eleventh W1-W3 verification-foundation iteration review](./channel-0.2-aw-iteration-review.md) — the
  first pass aimed at the design rather than at the machinery that checks it, and the first run under
  the 2026-09-04 ruling. Its frozen set reported nothing, and the instrument it built — the twenty-six
  properties evaluated over generated conforming vectors — is **green at 0 red over 2,000 vectors and
  52,000 evaluations**, with six injected violations each caught by the property that owns the rule.
  **AW1** is its one finding and the machinery raised it against itself: a retained iteration review
  had to record at least one finding, which the ruling had just made false for the outcome condition 4
  asks for. **AW2** is reported to the owner rather than corrected — AW1 belongs to neither population
  that ruling counts, because it was found by reading. AW is a **verification** family.
- [Twelfth W1-W3 verification-foundation iteration review](./channel-0.2-ax-iteration-review.md) — the
  first pass whose frozen set is **strictly larger** than its predecessor's, the generator having
  joined it under the quarantine rule; it reported nothing. Its method was the increment AW left,
  teaching the generator to refuse before dispatch, which reached `I4`'s first clause, `C5-P1`'s second
  and `C6-P1`'s second — three clauses that had no input at all while every generated interaction was
  dispatched and permitted. **AX1** is the entry points going stale in the commit that recorded the
  eleventh pass, and the class is closed by recomputing the condition-4 pass count and both next-work
  ordinals rather than by correcting the five sentences. **AX2** came out of mutating the generator:
  `dispatched` on an interaction record is read by **no property**, so a vector could state a dispatch
  its own timeline does not and every property stayed green. AX is a **verification** family.

## Disclosed process deviation in the T1-T4 correction

The totality review and the T1-T4 correction pass were performed in one session by
`agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`, on the repository owner's
explicit instruction, rather than by separate reviewer and author actors. This departs from the rule
above that a reviewer does not repair the design it reviews, and it is recorded here so the next
reviewer weighs the T1-T4 corrections knowing their author also wrote the attestation that found
them. The retained attestation itself is unmodified, and the independence requirement on the next
cycle is unchanged: its reviewer must differ from that identity and from all seven retained
reviewers.

The sixth and seventh reviews were both performed by reviewers separate from the correction author,
so this deviation was confined to the T1-T4 pass until the U1 pass below.

## Disclosed process deviation in the U1 correction

The eighth review and the U1 correction pass were performed in one session by
`agent:claude-opus-5-channel-0.2-closure-review-8-2026-08-14-3b27e3a`, on the repository owner's
explicit instruction, rather than by separate reviewer and author actors. It is recorded here rather
than left implicit, because an undisclosed reviewer-repairs-own-finding is precisely the defect class
this programme exists to catch.

Under the two-kinds-of-review section above, the correction pass and the
[U1 correction iteration review](./channel-0.2-u1-correction-iteration-review.md) that followed it are
legitimate author-side work rather than deviations. What remains a deviation is narrower and is the
part that matters: the actor that wrote closure review 8's attestation then corrected the blocking
finding that attestation raised. The next closure reviewer weighs the U1 correction knowing its author
also wrote the attestation that found it, and knowing the author had published a proposed fix before
being asked to apply it — so the correction was not derived independently of the review that motivated
it.

The retained attestation
[`channel-0.2-design-foundation-closure-review-8-attestation.md`](./channel-0.2-design-foundation-closure-review-8-attestation.md)
is **unmodified** by this pass and still reads as it did when the verdict was returned, including its
sentence that the design was not repaired there. That sentence was true of the review commit and is
superseded by this one; the attestation is retained rather than corrected, which is the policy for
every retained attestation.

The independence requirement on the next cycle is unchanged and now stricter by one name: its
reviewer must differ from all eight retained reviewers and from this correction author, which are the
same identity.

## Disclosed dispatch provenance of closure review 9

Closure review 9 was dispatched by a session that had itself already read the Channel 0.2 design
package and this policy, and that authored the immediately preceding correction commit
`fix(channel): close AD1 and AD3, the retained-record descriptions` as an author-side iteration pass.
This is recorded because an undisclosed relationship between a dispatcher and a reviewer is the same
class of defect as an undisclosed reviewer-repairs-own-finding, which this directory already
discloses twice.

What the dispatch did and did not carry is the part that matters. The brief named no artifact defect,
no area of suspicion, and none of the dispatching session's findings or reasoning; it pointed the
reviewer at `AGENTS.md` and this policy and told it to take its scope from them. The reviewer's
attestation records the disclosure in its own section, and records that the brief did narrow *where*
effort went — heavily toward C4, C8, C10, and the grid — while noting that **AE1 contradicts the
dispatching pass's own recorded result**: that pass ran a `C4-P2` evaluator and found nothing, because
it ran the property over the cases C4's narrative names rather than over the required vector group.
The finding that closes this cycle is therefore one the dispatcher had already looked for and missed,
which is the strongest available evidence that the reviewer's cold context did its own work.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all nine retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 10

Closure review 10 was dispatched by the session that **authored the commit it reviewed** —
`fix(channel): close AE1-AE5 and AD2 under the AE1 owner ruling`, including every artifact edit and
every verifier check in it — and that had also authored the preceding commit, the AD correction
iteration review, and the dispatch of closure review 9. This is the strongest such relationship this
directory records, and it is disclosed for that reason.

The brief named no artifact defect, no area of suspicion, and nothing about where the dispatching
session believed the work was weak or strong; it pointed the reviewer at `AGENTS.md` and this policy
and told it to take its scope from them. It also told the reviewer explicitly that it was reviewing
work whose author had arranged its review, and that this was a reason to probe the corrections harder
rather than defer to them.

The reviewer recorded the disclosure in its own section, recorded that the brief narrowed where effort
went — toward C4, C10, C12, the brief, and the grid, with less to C5-C7 and C11 — and recorded that
**AF1 sits inside the dispatching author's own change, in a place that author's own summary of updated
surfaces shows was not considered.** As with AE1 and the AD pass before it, the finding that closes
this cycle is one the dispatcher had the strongest possible incentive and opportunity to find and did
not. That is the available evidence that the arrangement did not soften the review; it is not proof,
and the next cycle should weigh it as evidence rather than as a guarantee.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all ten retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 11

Closure review 11 was dispatched by the session that authored the commit it reviewed
(`fix(channel): close AF1-AF8, the layer under the AE1 correction`), the two commits before it, and
the AD correction iteration review, and that dispatched closure reviews 9 and 10. The brief named no
artifact defect, no area of suspicion, and nothing about where the dispatching session believed the
work was weak; it pointed the reviewer at `AGENTS.md` and this policy, told it that it was reviewing
work whose author arranged its review, and told it to treat that as a reason to probe harder.

The reviewer recorded the disclosure, recorded what the dispatch narrowed, and recorded something the
two previous cycles could not: **its blocking finding was already documented by its predecessor.**
Review 10's AF1 named the completeness review among its evidence and quoted the row; review 11 proved
that row was still live rather than discovering it. The reviewer states this itself and draws the right
conclusion — that this cycle's evidence for "the cold context did its own work" is weaker than the two
before it, where the blocking finding sat in the dispatcher's own change and had not been named by
anyone.

Its two independently reached findings, AG2 and AG3, came from a question no earlier cycle had asked:
for each claim a correction makes about an artifact it did not edit, open that artifact. Neither was
visible from the diff.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all eleven retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 12

Closure review 12 was dispatched by the session that authored the commit it reviewed
(`fix(channel): close AG1-AG5 and sweep every finding's named artifacts`), the three commits before it
— the AF, AE, and AD corrections — and the AD correction iteration review, and that dispatched closure
reviews 9, 10, and 11. The brief named no artifact defect and no area of suspicion, told the reviewer
it was reviewing work whose author arranged its review, and told it explicitly that manufacturing a
finding to avoid committing to a conforming verdict was as much a failure as suppressing one.

**This is the cycle where that disclosure matters most**, because the verdict was favourable to the
dispatching author. Three things bear on how much weight it should carry. The reviewer returned six
findings rather than none, two of them in the correction it was reviewing. It flagged its own closest
escalation call — AH2 — and stated the condition under which its own verdict would be wrong, which is
not the behaviour of a review reaching for a clean result. And the owner then ruled the standard
*above* the verdict rather than accepting it, so the favourable outcome did not close the batch.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all twelve retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 13

Closure review 13 was dispatched by the session that authored the commit it reviewed
(`fix(channel): close AH1-AH6 and rule the closure standard`), the four commits before it — the AG,
AF, AE, and AD corrections — and the AD correction iteration review, and that dispatched closure
reviews 9 through 12. It also **recommended the 2026-08-15 closure-standard ruling, after the twelfth
review's verdict was known**, and that disclosure was carried in the dispatch brief so the reviewer
could weigh the standard it was operating under.

The brief named no artifact defect and no area of suspicion. The reviewer recorded that its blocking
finding was not in the set the brief pointed at, and it landed inside the dispatching author's own
change — the AH1 correction, on the operand that correction did not reach.

Two things the next cycle should carry. **AI9 means the programme's central claim was false for six
cycles**: S3's evidence surface was open while every entry point reported all findings closed, so a
reviewer should verify "every retained finding is closed" against the findings' own evidence rather
than against the indexes that assert it. And the correction author has now produced the same
propagation failure in six consecutive rounds; the sweep axis changed in this commit because of it,
with one round of evidence behind the change.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all thirteen retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 14

Closure review 14 was dispatched by the session that authored the commit it reviewed
(`fix(channel): close AI1-AI9 and sweep by concept`) and the follow-up commit that corrected the pin
clause's date, the five commits before them — the AH, AG, AF, AE, and AD corrections — and the AD
correction iteration review, and that dispatched closure reviews 9 through 13. It also recommended the
2026-08-15 closure-standard ruling after the twelfth review's verdict was known. This is the same
relationship the directory discloses for reviews 10 through 13, extended by one more correction commit
and one more dispatch.

The brief named no artifact defect and no area of suspicion, and told the reviewer it was assessing
work whose author had arranged its review. The reviewer recorded, in its own words, four things the
brief did narrow: verify the pin, run a property evaluator, check retained findings against their own
evidence, and read the previous attestation for form. It attributes **AJ2**, **AJ3**, and **AJ4** to
the third of those and claims no independent discovery of that class — only that it opened the
sentences. **AJ1's location, AJ5, and AJ6 are in no retained record**, and AJ1 sits inside the
dispatching author's own change, on the two artifacts that change's own check cannot read.

Two things the next cycle should carry. The **correction pass has now produced the same propagation
failure in seven consecutive rounds**, most recently inside the mechanism built to prevent it: the
sweep axis was changed to the concept in the AI commit, and the AI commit then computed the concept's
impact set from the artifacts it had been editing. The AJ pass executed the sweep as a recorded search
before editing, which is a further narrowing of the same failure and not a solution to it. And the
**AJ1 correction touched three artifacts the fourteenth review called correct**, normalising their
field lists so all six publish the reference identically; the field set is unchanged, and a reviewer
should confirm that rather than take it from this sentence.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all fourteen retained reviewers and from every correction author.

## Disclosed dispatch provenance of closure review 15

**This section was missing for a cycle.** The review 15 entry above ended "Its dispatch is disclosed
below" while this file's disclosures stopped at review 14; the disclosure existed, in review 15's own
attestation, and the pointer here resolved to nothing. It is written now rather than left as an
absence, and it is one of the two things the AL correction pass found for itself rather than taking
from the sixteenth review — the other is the operand enumeration's route-granularity surface, which
the review found independently as **AL2**.

Closure review 15 was dispatched by the session that authored the AD through AI corrections and the AD
correction iteration review, that dispatched closure reviews 9 through 14, that recommended the
2026-08-15 closure-standard ruling after the twelfth review's verdict was known, and that verified and
committed the AJ correction it did not itself write. The commit under review was written by an actor
with no prior history of this work, spawned because that dispatching session had produced the same
class of incomplete correction in seven consecutive cycles — so the dispatcher was no longer the author
of the commit under review, but remained the author of everything that commit corrects.

The brief named no artifact defect and no area of suspicion, and told the reviewer it was reviewing
work arranged by a party with an interest in it closing. The reviewer recorded that the dispatcher's
own verification covered exactly the ground where it found nothing, and that **AK1** lay outside all
of it, in the conjunct beside the one the dispatcher had corrected six times. Its full account is in
[the attestation's own dispatch section](./channel-0.2-design-foundation-closure-review-15-attestation.md),
which is the authority for it; this section is a pointer that now points at something.

## Disclosed dispatch provenance of closure review 16

Closure review 16 was dispatched by a session with **no prior involvement in this work**: it authored
none of the corrections, no artifact in the design package, no check in the design verifier, no
retained review, and no previous dispatch. It was a fresh session asked by the repository owner to
dispatch the round. This is the first cycle since review 9 in which the dispatcher is not the author of
the commit under review, and the first in the programme in which it is not the author of the work under
review at all.

That changes what the disclosure is worth in both directions, and the next cycle should weigh it
rather than read it as an improvement. Reviews 10 through 15 could each point at something specific —
the blocking finding sat inside the dispatcher's own change, which is evidence that the arrangement did
not soften the review. That evidence is unavailable here: a dispatcher with no stake also has no
demonstrated incentive it failed to act on. What is available is the narrower fact that the brief
conveyed no defect and no suspicion, and that the reviewer's own account of what the brief narrowed is
itemised in its attestation.

Before dispatching, that session read the git log, this policy in full, the commit messages of the two
most recent correction commits, and parts of the review 15 attestation. It made the clone, verified the
pin, and told the reviewer to re-verify rather than accept its numbers. Its brief gave five
instructions, each a restatement of a standing policy requirement — verify the pin, falsify rather than
read, re-derive retained findings from their own evidence, follow propagation, and audit the operand
enumeration — and the reviewer records which findings each produced: **AL2** and its class to the
third and fourth, **AL1** indirectly to the second, and negative results from the first and the fifth.

The same session then wrote this correction. That is the reviewer-adjacent relationship this directory
has disclosed five times in the other direction, and it is disclosed here in the direction it actually
runs: the corrections below were written by the party that arranged the review that found them, and
by a party that had formed no view of the design before doing so.

The independence requirement on the next cycle is unchanged and now stricter by one name: its reviewer
must differ from all sixteen retained reviewers, from every correction author, and from this
dispatching session.
