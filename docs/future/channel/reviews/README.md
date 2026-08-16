# Channel 0.2 design-foundation reviews

Status: four owner rulings resolved; thirteen retained independent reviews, twelve negative and one
conforming with nonblocking findings — which under the 2026-08-15 closure-standard ruling did not
close the batch. The thirteenth raised blocking **AI1** with nonblocking **AI2**-**AI9**. B1-B4, N1-N3,
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
`conforms-with-nonblocking-findings` with **AH1**-**AH6** and no blocking finding. Every finding this
programme has recorded is
now closed in the artifacts it was raised against. One piece of named residual work is open and is not
a finding: of the twenty-five properties the package states, eleven capabilities owe the required-green
set that **AE3** made a normative field, and the thirteen state-machine properties AF7 brought into
the audit owe it too — with `I1`-`I7` owing a named mutation as well. None of that is a verdict: a
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

The classification is **declared here rather than inferred from the prose below**, and the design
verifier requires it to be total: every family the policy names must appear in this table, and every
`iteration` family must have its retained record. AD2 was ruled a defect because a comment claimed a
class over code that tested two literals; its replacement derived the class from one sentence shape in
the next-work steps and missed `V` entirely along with `W5` and `W6`, which is AF6 — the same defect
an order of magnitude smaller. A declared, totality-checked table is what removes the wording
dependency instead of narrowing it.

| Family | Raised by | Record |
| --- | --- | --- |
| B | closure-review | original design-foundation attestation |
| N | closure-review | first closure attestation |
| F | closure-review | final closure attestation |
| D | closure-review | definitive closure attestation |
| T | closure-review | totality closure attestation |
| R | closure-review | closure re-review attestation |
| S | closure-review | closure review 7 attestation |
| U | closure-review | closure review 8 attestation |
| V | iteration | U1 correction iteration review |
| W | iteration | W correction iteration review |
| X | iteration | W correction iteration review, first pass |
| Y | iteration | W correction iteration review, second pass |
| Z | iteration | W correction iteration review, third pass |
| AA | iteration | W correction iteration review, fourth pass |
| AB | iteration | W correction iteration review, fifth pass |
| AC | iteration | AC correction iteration review |
| AD | iteration | AD correction iteration review |
| AE | closure-review | closure review 9 attestation |
| AF | closure-review | closure review 10 attestation |
| AG | closure-review | closure review 11 attestation |
| AH | closure-review | closure review 12 attestation |
| AI | closure-review | closure review 13 attestation |

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

The thirteenth review has run, from a fresh isolated clone, and returned `does-not-conform` with
blocking **AI1** and nonblocking **AI2**-**AI9**; its retained record is
`channel-0.2-design-foundation-closure-review-13-attestation.md`. **Steps 1 through 3o are complete.**
Step 4 is the live path, and the next agent reviews the AI corrections.

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
4. Obtain another fresh independent review of the corrected pin, from a reviewer identity distinct
   from the correction author and all eight retained reviewers, **in a fresh isolated clone**. Its
   scope, verdicts, and probe requirements are unchanged from the sections above. It writes only its
   own attestation.

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
   - `build/verify-doc-links.ps1`;
   - `build/verify-text.ps1`; and
   - `build/verify-interchange.ps1`.

Only after the conforming attestation, closure record, documentation/status updates, and clean full
gate are committed may the next agent start Batch 2 from the
[neutral contract brief](../Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md). This closure
authorizes planned schema work; it does not ratify Channel 0.2 or claim implementation conformance.

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

- [Closure review 13](./channel-0.2-design-foundation-closure-review-13-attestation.md) — reviewed
  `e7bfeba6ba58e2e4e9a48a5148e2461c187bf452`; `does-not-conform`; blocking **AI1** with nonblocking
  **AI2**-**AI9**. **Its isolation is complete**: a fresh isolated clone, 890 tracked paths, reviewer
  identity distinct from all twelve earlier reviewers and every correction author. It recomputed all
  twelve registry pins, enumerated the grid to 108 cells and 180 pairs agreeing with reviews 7-12, and
  recorded three probes that found nothing. **AI1** is the AH1 decision propagated to one operand of
  two, and **AI9** is a retained finding — S3's own evidence surface — that had stayed open for six
  cycles while every entry point reported the programme's findings closed. Its dispatch is disclosed
  below.
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

The current review target is the commit titled `fix(channel): close AI1-AI9 and sweep by concept`,
committed 2026-08-15, which is the head of the correction sequence beginning at
`fix(channel): make C4-P2 falsifiable`. Review that commit or any later commit whose design
artifacts hash identically to it — and check that claim rather than assuming it, because this clause
has now gone stale twice: the eighth review raised it as **U6**, and the rewrite that closed U6 was
itself superseded one commit later and raised as **X6**. The design verifier now compares this
sentence against the most recent commit that changed a design artifact, so a correction pass that
forgets it fails the gate rather than misdirecting a reviewer. The preceding pins
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
