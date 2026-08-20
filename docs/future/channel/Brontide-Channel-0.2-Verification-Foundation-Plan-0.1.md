# Channel 0.2 verification foundation plan 0.1

Date: 2026-08-17

Status: next-work plan, adopted by owner decision on 2026-08-17. **It is not a first-batch design
artifact**, it is not part of the reviewed package, and no closure review assesses it. It records why
the closure cycle is being paused, what has to exist before it resumes, and how to tell whether that
work succeeded.

Closure-cycle state: **on-hold** since 2026-08-17, at 16 retained attestations.

That line is the declaration, and this document owns it. The design verifier reads the state from
here, requires the review policy's step 4 to carry the matching do-not-dispatch marker, and **fails if
the reviews directory holds more attestations than the number above** — so a closure review run and
retained while the cycle is held fails the gate rather than being noticed later. What the gate cannot
see is the dispatch itself, which happens in another clone; it catches the retention, which is the
first moment the work becomes visible in this repository. The instruction in step 4 is still the
primary control. Resuming the cycle means changing the state here, against the conditions in section 3,
and removing the marker there; editing the number to match a retained attestation is not resuming the
cycle, and the check says so.

Owner decision, stated first because everything below follows from it: **the next independent closure
review is on hold until verifying this design is stable and cheap.** Sixteen reviews have run and
fifteen returned `does-not-conform`. The programme's throughput problem is not reviewer diligence or
correction care; it is that the only instrument able to find a real defect here is a human reading
prose, and the surface that reading has to cover grows every time a defect is corrected.

What the hold does **not** change:

- every correction through **AL1**-**AL4** stands, and the design gate is green at this commit;
- Batch 2 remains closed. This is not a shortcut to schema work — it is a decision to fix the
  measuring instrument before spending another cold-context review;
- the retained attestations, the closure standard, and the four owner rulings are untouched; and
- the independence requirements in the [review policy](./reviews/README.md) are unchanged for
  whenever the cycle resumes.

## 1. Why closure has not converged

Ten independent reviews have run since the batch was first believed finished (review 7). **Every one
produced findings.** Blocking count by cycle: 1, 1, 1, 1, 1, 0, 1, 1, 1, 2. The single zero is review
12, whose `conforms-with-nonblocking-findings` verdict the 2026-08-15 closure-standard ruling holds
does not close the batch. There is not yet a cycle in which a cold reviewer looked and found nothing.

Five causes, in the order they cost time.

### 1.1 The design has no executable form

Twenty-six capability-wide properties are stated in English across eleven artifacts. `C4-P2` is a
two-conjunct statement about frame ordering whose operands are defined in five other documents. The
design verifier is over two thousand lines and checks **structure and strings** — that a field list
appears verbatim in every surface registered to carry it, that a status block names the newest finding
family, that a count in prose matches what a directory holds. It cannot check whether a property is
true, whether it can fail, or whether it stays green on conforming behaviour, because nothing executes
it.

So the detector for the defects that actually matter is one careful reader, once, with a cold context.
`U1`, `AE1`, `AC1` and `AL1` were each found that way and none was reachable by any check in the file.
Worse, each reviewer **writes an evaluator from the published prose, uses it, and throws it away** —
reviews 8, 9, 12, 15 and 16 all did. That is the single most productive tool this programme has and it
has never been kept.

### 1.2 One fact is published in six places by design

Each artifact must be readable alone, so the same fact is restated in the contract, both machines, the
grid, the matrix, the ledger and the brief. That is good for a reader and it means **every change to a
fact is a manual six-site edit with no single owner**. The gate's answer has been a check that the six
restatements agree verbatim.

`AI1`, `AJ1`, `AK1` and `AL2` are one event repeated: a fact changed, the edit reached some of its
sites, and the check written to catch that could only see the sites its author already knew about.
Nine consecutive cycles have carried an instance of this shape. It is a normalization problem being
managed with string comparison, and no amount of correction discipline retires it.

### 1.3 Corrections enlarge the review surface

Every fix adds sentences — a qualifier, a disposition note, a status-block paragraph explaining what
changed and why. Every added sentence is new material for the next reviewer. The `U`→`Z` sequence is
twenty layers, each existing to guarantee the one above it and each with its own hole. The target
grows as it is repaired.

A small but exact illustration from the `AL` pass: the paragraph written to *describe* the new
record-publication sweep tripped that sweep, because to a string matcher a description of a record and
a publication of one are the same thing.

### 1.4 Every check is one defect behind

A check is written from the shape of the finding that produced it, so it catches that class and not
the next one. The `AK7` recognizer matches a declared fact's own words inside a property; `AL1` walked
past it by reading a session's state *through a transition of it* rather than naming it. The `AK1`
guard keys on a reference's name; `AL2` walked past it in two cells that never use the name. Coverage
grows one class per cycle, which is the same rate the defects arrive.

### 1.5 The bar is "an adversarial cold reader finds nothing"

Not "the design is sound" — zero findings of any severity, across twelve scope areas, 108 grid cells
and eleven artifacts, from a reviewer instructed to hunt and told that manufacturing a finding and
suppressing one are equal failures. For a prose specification this size the probability of zero is low
even when the work is good. This is a deliberate standard and it is not being relitigated here; it is
listed because it multiplies the cost of every cause above.

## 2. The work

Four items, ranked by how much of the recurring failure each retires. **W1 and W2 are the ones that
change the arithmetic**; W3 and W4 are cheap and help.

### W1 — one owning artifact per fact, citations everywhere else

**Problem.** Section 1.2. Six verbatim restatements per fact, maintained by hand.

**Work.** For each fact a property reads, name exactly one artifact that states it. Every other
artifact cites it — a link and a name, not a copy. Start with the three frame references and the
`unseen` refusal record, which are where the last nine blocking findings live, then the settling-frame
and terminal-frame field lists, then the per-session facts C12 declares.

**Acceptance.** The frame-reference registry in `build/verify-channel-0.2-design.ps1` — its surface
lists, its exact-count assertion, its abbreviated-publication sweeps — is **deleted** and replaced by
a citation-resolution check: every citation resolves to the owning artifact's statement, and no
artifact states an owned fact itself. If that deletion cannot be made, the normalization did not
happen.

**Cost of not doing it.** One further instance of the `AI1`/`AJ1`/`AK1`/`AL2` family per cycle, at
roughly one cycle each.

**Risk, stated plainly.** Standalone readability is why the duplication exists. A reader of the grid
alone should still learn what the `unseen` cells record. Mitigation is that a citation carries the
fact's *name* and a link, so the reader knows exactly what to open; the alternative on the current
evidence is that the six copies disagree once per cycle.

### W2 — properties that execute

**Problem.** Section 1.1. The properties are prose, and every reviewer builds and discards the same
evaluator.

**Work.** Give each of the twenty-six properties a machine-readable statement, its named mutations,
and its required-green set, and run them in the gate. This is the same work as filling the
required-green sets `AE3` made a normative field — twenty-five of twenty-six still owe theirs, and
`I1`-`I7` owe named mutations as well — so it is on the critical path regardless of this plan.

**Acceptance.** `build/verify-channel-0.2-design.ps1 -NegativeProbe` is no longer the only executable
falsification in the repository. Each property is red on each of its named mutations and green on
every member of its required-green set, in the gate, on every commit. `C4-P2`'s eleven-input evaluator
from closure review 16 is the worked example and should be reconstructed first, from the attestation's
own description.

**Sequencing note, and the one real obstacle.** Vectors are Batch 2's artifact and Batch 2 is closed,
so this must be written against the neutral brief's vector *format* rather than against authored
vectors — property statements and mutations only, with hand-written inputs. That is a smaller thing
than Batch 2 and does not authorize it.

### W3 — keep disposition history out of the design artifacts

**Problem.** Section 1.3. Status blocks now carry paragraphs of finding history, and that history is
itself reviewable surface.

**Work.** An artifact's status block becomes a pointer: what the artifact is, what it awaits, and a
link to the record. Disposition history lives in the review record, where it already lives anyway.

**Acceptance.** Every status block fits in five lines. The `AI4`, `AG4`, `AJ5` and `AH4` checks — all
of which police status-block freshness — collapse into one check that the pointer resolves.

### W4 — re-scope the closure gate (owner call, not a task)

The standard in force is that only an unqualified `conforms` closes. With W1-W3 landed, a finding
would mean a real design defect rather than a propagation slip, which is the condition under which
that standard is cheap rather than expensive. **Recommendation: keep the standard and change what a
finding costs to produce, rather than lowering the bar.** No action is proposed here; it is recorded so
the question is visible when the cycle resumes.

## 2a. What W2 has landed

Recorded here, in the document that owns the plan, rather than in a design artifact's status block --
W3 exists because those blocks are where disposition history accumulates, and this work would be the
next paragraph in them.

**`C4-P2` executes.** `build/verify-channel-0.2-properties.ps1` runs in the repository gate on every
commit and evaluates the property over eleven hand-written inputs: both named mutations, all seven
required-green members, and the two vectors **AK1** and **AK5** were raised for. The verdicts are the
eleven closure review 16 recorded by hand in its probe P2, and the evaluator that produced them is
kept this time instead of being thrown away. The declaration is
`conformance/channel-0.2-properties.json`; the inputs are `conformance/channel-0.2-property-vectors.json`.

**The operands are checked by mutation, not by assertion.** Nine operand mutations reproduce review
16's probe P3: each reverts one published field of one record and re-evaluates. Three flip a verdict
and six do not, and both outcomes are asserted -- `AK1`'s session and `AK5`'s arrival ordinal each take
a conforming vector red on their own, `AK6` moves nothing alone and moves the duplicate terminal red
once `Y4`'s ordinal is reverted with it, and the fields review 16 recorded as redundant are recorded
here as redundant. A correction whose operand stops being load-bearing now fails the gate rather than
waiting for a reviewer to notice.

**The executable form cites the artifacts and does not restate them.** Every required-green member,
every named mutation, and the count of legal members in the group are checked against the capability
contract's own words; the required-green field is checked against the neutral brief that makes it
normative. This file is deliberately not a twelfth surface publishing the same facts -- that is the
failure W1 exists to retire, and the citation checks fail when the two disagree rather than letting a
second copy drift. Section 4's count of executable properties is checked against what actually
executes, so the measure cannot go stale in the direction that flatters the work.

**All fifteen properties condition 2 names now execute.** `C4-P1`, `C4-P2`, `S1`-`S6` and `I1`-`I7`
run in the gate on every commit: **69** evaluations over **30** declared inputs, plus the nine operand
mutations. Each has at least one named mutation it goes red on and a required-green set it stays green
on, and a property green on every input fails the gate as a finding against the property.

**The fourteen owed required-green sets are stated.** Every one names the same two members: the
conforming single-session realization, and two sessions conforming with the second establishing and
admitting after the first drains. The second is the point. It is the vector **AL1** and **AK7** were
raised for, and a property that reads one session's fact across the vector is green on the first and
red on it -- so the defect that took two cycles to find by reading is now a gate failure. Breaking
`S3` back to its pre-AL1 form was run as a probe and produces exactly that: red on the two-session
member, green on everything else.

**`I1`-`I7` gained named mutations, which they owed as well.** Seven cells read `owed` in both
columns; the interaction machine committed those properties to no probe at all, which the
completeness review recorded as a larger gap than the eleven `owed` required-green cells. Each now
has one.

**`C4-P1` is filled with its scope stated rather than assumed.** Its set is scoped to the one named
profile, where one endpoint initiates both classes and the session-wide and per-direction readings of
the in-flight bound coincide. Under **AE3** the direction-scope disagreement is a known
conforming-realization exposure rather than an unwritten set, and a profile in which both endpoints
initiate must still state which reading it means before its vectors can be written. This set does not
decide that for it, and says so. Filling it silently is what would have reproduced the omission the
completeness review warns about. `C4-P1` also gained a third mutation: the contract names one per
clause for two of its three clauses, and a clause whose mutation no group contains is unfalsifiable in
the suite however well the contract states it.

**Pinning found a second surface immediately.** `S1` reads the legal session transition table, so the
evaluator carries a copy of it -- W1's problem arriving in the gate. It is pinned in both directions
against the session machine's own table: an edge here the artifact lacks would leave `S1` green on an
illegal transition, and an edge there this file lacks would take `S1` red on conforming behaviour.
Both were probed.

**All twenty-six properties now execute, and no cell in either audit table reads `owed`.** The eleven
per-capability properties outside condition 2 -- `C1-P1`, `C2-P1`, `C3-P1`, `C5-P1` through `C12-P1`
-- were the last of them. The gate runs **113** evaluations over **41** declared inputs plus the nine operand
mutations, and each property is red on the mutation the completeness review already named for it and
green on both required-green members.

**Two capability properties are evaluated by calling the machines' rather than restating them.**
`C2-P1` is `S1` and `S4` at capability level and `C8-P1` is `I2` and `I3`. Writing the claim twice
would be the duplication W1 exists to retire, arriving inside a verifier instead of inside prose, and
the second copy is what goes stale.

**Three things are recorded as limits rather than closed, because stating a set is not the same as
settling a question.** `C4-P1` and `I5` carry sets scoped to the one named profile, since the
direction scope of the in-flight bound is undecided for a profile in which both endpoints initiate.
`C12-P1`'s second clause is a claim about the declaration set rather than about any vector and is
evaluated once, over that set. Its third clause -- that neither stack imports the other's semantic
runtime -- is not a fact a vector carries and is delegated to the repository's dependency guards; the
declaration says so rather than pretending to evaluate it.

## 2b. What W3 has landed

**The nine status blocks carry no disposition history.** Each states what the artifact is and what it
awaits, in five lines, and links to its section of the
[disposition index](./reviews/channel-0.2-disposition-index.md) -- a retained review record, not a
design artifact. Between them those blocks held **265 lines** at `9ce01a0` and now hold **45**,
counted the way the check counts them: from `Status:` to the blank line that ends the paragraph, blank
lines excluded. This sentence said 289 for three commits and no reading of that commit reproduces it --
265 by the check's own reader, 316 read to the first heading, 283 lines deleted from those blocks by
the W3 commit. It is corrected under **AM2** and both halves are now recomputed by the gate. Nothing was
rewritten: every section of the index is the text that stood in that artifact's block at commit
`9ce01a0`, moved verbatim, with relative links re-based one directory and nothing else touched. A
move that paraphrased would be a fresh statement of a fact the attestations already own.

**One check replaced two, and it bounds the surface rather than tracking it.** **AI4** asked nine
status blocks whether they reached the newest family and **AH3** asked the plan's separately, because
the plan puts its title heading above its status line and the block reader returned nothing for it.
The replacement asks four things once: the block is five lines or fewer, it links to the index, the
link resolves to a section, and that section reaches the newest family. The length bound is the half
that matters -- a block that keeps its pointer and grows a paragraph beneath it is the old state with
a link added, which is how every previous correction to these blocks went.

**Four checks were reading the moved prose, and only two of them were named here.** W3's acceptance
sentence names AI4, AG4, AJ5 and AH4; what actually depended on the status blocks was AI4, AH3,
AB1's family sweep over the plan, and AJ2's narrative check. All four now read the disposition index,
which is the collapse this item predicted arriving at a different set of checks than it named.

**Moving the history found a check answering the wrong surface.** The neutral brief's required
adversarial groups were verified by searching the whole brief for "intra-interaction frame order and
its ordering mutation", and the only passage carrying that phrase was the status block's own account
of what the V-Z corrections had done. A check on the vector groups was being answered by a sentence
about a correction to them, and it went green with the groups section saying something weaker in the
singular. It is now scoped to that section and matches what the section says: **both** ordering
mutations, one per conjunct.

**The Channel index's eleven rows are collapsed too, and AG4, AH4 and AJ5 are retired.** Those rows
carried the same history in another form -- 8,746 characters across eleven cells, averaging 795 each.
They now state what the artifact is for and point at the disposition index, and the nine artifact rows
run about a hundred characters. The three checks went the way AI4 and AH3 did: AG4 required each row
to name the newest family or say the artifact was unchanged by it, AH4 closed the escape clause bound
to no family, and AJ5 closed the escape naming one finding of a family as though it spoke for the
family -- three checks over one surface, each written from the shape of the finding before it, which
is section 1.4 exactly. One check replaces them: every row points at the index, and a row's state
column is bounded. The freshness question is asked once, of the index.

The Design reviews row is bounded higher than the rest and the reason is stated in the check rather
than fudged: **AE4** requires it to name every family a retained iteration review records, because a
pass once denied records that existed. That enumeration is a fact about what the directory holds, not
disposition history, so it is what the row is for.

**Nothing was lost.** Every finding token and every clause of the eleven rows is present in the
disposition index, verified rather than asserted, the same way the status blocks were.


## 2c. What W1 has landed

**The frame-reference registry is deleted.** That was this item's acceptance and it is met: 169 lines
of surface lists, exact-count assertions and abbreviated-publication sweeps are gone from
`build/verify-channel-0.2-design.ps1`, which drops from 2,377 lines to 2,257.

**It was not done by citation.** Section 5's open question 1 asks whether standalone readability
survives W1, and calls that the question deciding whether this item is real. It is not answered here
because the implementation does not need it answered: the duplication survives for the reader and
dies for the maintainer. The four facts are owned by `conformance/channel-0.2-facts.json` and
**rendered** into all **21** publication sites across **6** artifacts, each a fenced region the
artifact carries:

    <!-- fact:NAME -->its kind, its **session**, ...<!-- END -->

with `NAME` the fact's id and the closing marker `/fact`. The markers are written as placeholders
here because a real pair in this document would be a real publication, and the gate says so --
which is how this paragraph was caught on its first run.

A reader of the grid alone still learns what the `unseen` cells record, in the same words as before.
No human writes the second copy. Changing a field is one edit to the declaration and
`build/verify-channel-0.2-facts.ps1 -Apply`, which rewrites every site of that fact and touches no
site of any other; this was tested by adding a sixth field, applying it, and reverting.

Both counts above are **recomputed by the facts gate**, and read `twenty` and `five` until **AN4**.
Those were the frame references' own numbers at `2684ec7`, correct when this section was written and
left standing one section later when the `unseen` refusal record became the fourth owned fact -- by
the same paragraph that records the change. Section 4's copy of the count was corrected to 21 and this
one was not, which is this programme's recurring shape arriving in the document that exists to end it,
in a number describing the mechanism that ends it.

**The surface list is not in a verifier any more.** A fence *is* the registration and it lives in the
artifact, so a check cannot be scoped to the surfaces its author already knew about -- which is the
shape **AI1**, **AJ1**, **AK1** and **AL2** each had, and the reason the registry's own comments kept
saying a guard scoped to what it can read certifies its own completeness.

**One thing fencing cannot do, and what was done instead.** A fence registers a surface that exists,
so a surface deleted outright removes its own registration and leaves nothing to notice the absence.
A probe confirmed that: deleting one of the grid's three `unseen` publications passed every check. The
exact count is therefore kept, for the reason AI1 gave -- a lower bound is what let that check certify
its own scope -- but moved beside the fact in the declaration and checked in both directions, so a
fence in an undeclared artifact fails as loudly as a missing one.

**The `unseen` refusal record is owned too, and with it condition 1 is met in both its halves.** The
record is what `C4-P2`'s first conjunct quantifies over, and until now it was hand-maintained prose at
every surface: its provenance, its detailed reason and its effect certainty were written out by three
artifacts and watched by the design verifier's AL2 sweep, while only the reference inside it was
rendered. It is now one declared fact whose four contents include the refused-frame reference
**nested** rather than restated -- a field written `@<id>` renders another declared fact in place --
so the record and the reference cannot drift apart, which is the failure this work exists to end
arriving one level up.

It is rendered into five surfaces: C10, which owns it, the interaction machine's recipient `unseen`
row, the grid's two `unseen` cells, and the responsibility matrix row that owns the observation
record. Two of those five stated the record with a field the others had and they did not, and the
rendering settles both in the direction the package already held. C10
did not state the effect certainty, which the completeness review's operand enumeration nonetheless
names C10 as a publishing surface for; and the interaction machine's row put the provenance and the
detailed reason on the peer fault rather than on the observation, where C10 and the grid both put
them. Neither is a choice between defensible designs, so neither is an owner question: the row now
records the observation and says in its own words that the fault's provenance and detailed reason are
those the record carries.

**The AL2 sweep is deleted from the design verifier rather than extended**, which is this item's
acceptance for the record as the registry's deletion was for the references. It moves to
`build/verify-channel-0.2-facts.ps1`, beside the declaration, keyed to a trigger and co-terms the
fact declares instead of to a field list written out in a verifier. It also loses its neighbour
exemption: a fenced publication anywhere in the window used to excuse the passage beside it, and the
AL2 instance was *two adjacent cells in one table row*, so abbreviating either one alone put the
other's fence inside the window. The trigger is a value the rendering itself carries, so every
occurrence that survives the fence sentinel is already outside every fence.

**Run against this commit's parent it fires on three surfaces** — the interaction machine's `unseen`
row and both grid cells — and nowhere else in the sixteen files it sweeps. That parent is the
AL2-corrected package, at which the design verifier's own sweep is green: each of those three rendered
the reference inside a fence, which is what that sweep asked for, and hand-wrote the record's other
three contents beside it, which is what it did not ask about. The three are the surfaces this change
converts, so the check was written against a state where it fails and the correction is what makes it
pass.

**The fifth surface is the one no sweep can reach, and it is why the enumeration was done by reading
as well.** The responsibility matrix row that owns the observation record named two of the record's
four contents -- a provenance and the reference -- and never used the detailed reason the sweep
triggers on, so it is invisible to a trigger-and-co-terms check by construction. It is the surface
AJ1 was raised against for the settling frame, one fact later. It publishes the whole record now, and
what generalises is the limit rather than the fix: a sweep keyed to a fact's own words finds passages
that state most of it, and a passage that states a *third* of it is found by someone opening the
artifact that owns the concept and asking which of its rows are about this fact.

**One defect in `-Apply` was found by using it.** `Set-Content -Encoding UTF8` writes a byte-order
mark unconditionally under Windows PowerShell, so rendering a fact into a BOM-less artifact changed
its first three bytes; the rewrite also emitted CRLF into artifacts stored with LF, and rewrote
*every* fence in a file it touched rather than only the ones that disagreed, putting unrelated sites
in the diff. All three are fixed, and the tool is now byte-faithful to everything it was not asked to
change. The first two were visible only in `git diff` and neither would have been caught by any check
here, which is worth recording: the generator's own output is not covered by the mechanism it serves.

**What W1 still owes.** The per-session facts C12 declares and the settling-frame and terminal-frame
field lists in the completeness review's operand enumeration are stated once each and not yet fenced.
Condition 1 does not require them and no cycle has produced a finding in one; they are the obvious
next members if a cycle does.

## 2d. What the condition 4 pass found

The author-side pass condition 4 names has run, at `88f2447`, and is retained as the
[W1-W3 verification-foundation iteration review](./reviews/channel-0.2-am-iteration-review.md). It
raised **AM1**-**AM3** and corrected all three, so **it does not meet condition 4**, which requires a
pass that finds nothing it can fix.

**This section is that family's disposition record**, under the owner ruling of 2026-08-20 recorded in
the [review policy](./reviews/README.md#finding-family-provenance). AM is the first family raised
against the verification work rather than against the design, and the policy's provenance table now
declares that axis for every family: a `design` family is dispositioned in the completeness review's
history, a `verification` family here. Neither is exempt. The ruling exists because recording AM in the
design history made *the newest family* one that had touched no design artifact, and the newest family
is the anchor for five freshness checks — which were then all answered by nine sections saying
"unchanged by AM". A plain exemption was rejected as an escape clause of the kind AH4 and AJ5 already
had to be closed for; the backstop is that a `verification` family may not be named by any design
artifact, so a misclassified finding whose correction reached the design fails the gate.

Its method is the part worth carrying forward, because none of the three came from reading. It
recomputed every number these sections state, re-derived every claim one document makes about another,
and broke every guard W1, W2 and W3 added to see whether the gate names the defect. The last of those
found **AM1**: the status-block length bound was read to the first blank line, and the disposition
history it exists to exclude had sat one blank line beneath — a paragraph appended there passed every
check in the gate, probed and green. The bound is now over the whole region between `Status:` and the
first section heading, with a declared front-matter permit list for everything in it that is not the
block, so an unrecognised paragraph fails rather than passing quietly.

**AM5** was found by CI rather than by this pass, and is the sharper of the two. The review-target pin
was checked by commit subject against `git log -1` over the eight design artifacts, and a
`pull_request` build runs that on the **merge commit** while the local gate runs it on the linear
branch. This branch changed a design artifact and changed it back, so the merge is TREESAME to `main`
for those paths and the two views name different commits — and the merge view is the one that matters,
because it is what `main` reports after merging. The check now compares the design artifacts' blob
hashes at the pinned commit against the tree a reviewer reads now, which is what the policy's clause
says and gives one answer in both views. A guard whose answer depends on which view of history runs it
is a guard that cannot hold, and no local run could have found it.

**AM4** is the pass turning its own method on its own correction, and is why the AM2/AM3 fix is
trustworthy at all: the recomputation of a *historical* measure fetched the blob with `git show`, and
Windows PowerShell decodes native output with the console code page, so every em dash in these
artifacts arrived as three characters. The check measured 8,754 at a commit holding 8,746 -- failing a
correct claim, and it would have passed a wrong one that erred the same way. Historical blobs are now
read through an explicit UTF-8 decoder. A verifier that reads history reads through a decoder nobody
chose.

**AM2** and **AM3** are section 4's two hand-measured numbers, both wrong; they are corrected in place
above and both halves of both are now recomputed from the repository by the design verifier. The split
is the evidence worth keeping: of the five measures, the one the properties gate computes was right,
the fact-surface count was right, and **both numbers left to prose were wrong**. That is this document's
own argument, measured on this document.

What the pass verified rather than believed is recorded in the review: the W3 move is verbatim, both
moves lost no finding token, every W2 count is exact, and the W1, W2 and W3 guards fire when broken.

## 2e. What the second condition 4 pass found

The second author-side pass has run, at `0e43a69`, and is retained as the
[second W1-W3 verification-foundation iteration review](./reviews/channel-0.2-an-iteration-review.md).
It raised **AN1**-**AN6** and corrected all six, so **it does not meet condition 4** either. **This
section is that family's disposition record**, under the same 2026-08-20 owner ruling that routes a
`verification` family here, and the
[provenance table](./reviews/README.md#finding-family-provenance) declares AN on both axes.

**It began by re-running every probe the AM pass recorded, which is what the review policy's next-work
paragraph asks of it, and all of them reproduced.** None of the six findings came from that. Every one
came from the question that pass did not ask: *where else is this stated?*

**AN1** and **AN2** are two guards whose scope is narrower than the question their own comments claim,
and each sits inside a check written to close an earlier instance of the same class.

- **AN1** -- W3's whole claim is that a status block may carry no history because it carries a
  pointer, and the check's comment says *"the pointer must RESOLVE"*, as does section 2b above. It did
  not ask that: it looked the section up by the artifact's file name and never read the anchor.
  Renaming a heading in the disposition index -- which gains a section every time a family is
  dispositioned -- left every pointer to it dead with the whole gate green, probed. Deleting the
  section outright, which is the probe the AM pass ran, is the one instance of the class a by-name
  lookup happens to catch. Corrected at two scopes: `build/verify-doc-links.ps1` now resolves every
  Markdown fragment in the repository against the headings of the document it points at, and the
  design verifier resolves the pointer each status block and index row actually carries and requires a
  block's pointer to land on the section about that artifact.
- **AN2** -- **AM5** rewrote the review-target pin to compare the design artifacts' blob hashes at the
  pinned commit against the tree a reviewer reads now. It compared **eight of the nine**: the list was
  written out a second time in the verifier, and the artifact it omitted is the redesign plan, item 3
  of the review policy's required review scope and the home of the four owner rulings and the closure
  standard. A commit changing only that artifact left the pin green, probed by making one. That is
  U6 -- a reviewer sent at artifacts that have already moved -- inside the check written to close U6.
  Corrected by deriving the pathspec from the one list the rest of the file already uses.

**AN3**-**AN6** are four facts corrected in the record that owns them and left standing in one to four
other records. **AN3** is section 4's third measure: its history was left entirely to prose while the
two measures beside it were being recomputed for having been left to prose, it claimed the count *"fell
at each step"* where it rose at two of them, and three of its four deltas are produced by no reading of
the commits they describe. It is now a per-commit table this verifier recomputes. **AN4** is the
publication-site count, stated as twenty in four places and corrected to 21 in one; the facts gate
counts it now. **AN5** is the status-block line total, which AM2 corrected from 289 to 265 and declared
to have two surfaces -- there were four, and the disposition index carried 289 for three commits after
the correction. **AN6** is the pin clause's own description of the mechanism AM5 replaced.

**Two things generalise, and the second is the one to carry forward.** A guard's comment is a claim,
and two of six findings are comments claiming a stronger question than the code asks -- reading each
guard's comment against its code is a pass nobody here has run in full. And **recomputing a number in
the document that owns it says nothing about the documents that repeat it**: the AN5 sweep found a
fourth surface of its own subject on its first run, after this pass's reading had found three.

## 2f. What the third condition 4 pass found

The third author-side pass has run, at `d01e706`, and is retained as the
[third W1-W3 verification-foundation iteration review](./reviews/channel-0.2-ao-iteration-review.md).
It raised **AO1**-**AO3** and corrected all three, so **it does not meet condition 4** either. **This
section is that family's disposition record**, under the 2026-08-20 owner ruling that routes a
`verification` family here.

It took the brief the AN pass left it — a guard's comment is a claim, and testing each one against its
code is a pass nobody had run in full — and read the 103 comment blocks in the three gates that make a
testable structural claim. **AO1 came out of the fifteenth of them, and it is the most serious finding
this verification work has produced.**

- **AO1** — `S1` is evaluated against a copy of the session machine's legal transition table, and the
  cross-check that keeps the copy honest states its own claim exactly: *"the artifact must declare no
  accepted edge this file does not carry. A row added there and forgotten here would make `S1` red on
  conforming behaviour."* The artifact's table has ten rows and the reader saw eight: the last two say
  **`any nonterminal`** in the From cell — a fatal recognized Channel violation and a transport or
  process loss, both to `faulted` — and the reader required a backticked state there. So `S1` and
  `C2-P1` were **red on a session that faults from `established`**, which every column of the coverage
  grid's `established` row routes to `faulted`. That is **AE1**'s defect, the one that took ten cycles
  to find, in a gate written after the lesson, reached through the guard written to prevent it.
  Corrected by parsing the From cell over the states the machine declares rather than matching it, by
  failing on a cell the parser cannot read rather than dropping the row, by adding the three missing
  edges, and by retaining the vector `S-conforming-fault-from-established` as an additional-green
  member of all twenty-five properties the conforming single-session vector belongs to, so the false
  red is pinned by an input and not only by a comparison of two lists.
- **AO2** — section 2a states what the properties gate runs in four prose numbers, and the AO1 vector
  moved all four. The section 4 measures beside them have been recomputed since AM2, AM3 and AN3;
  these are the same kind of number about the same runs and were never included. Both sentences are
  recomputed by that gate now.
- **AO3** — **the probes are prose, and three passes have rebuilt them by hand.** Section 1.1 of this
  plan says every closure reviewer wrote a property evaluator, used it, and threw it away, and calls
  that the single most productive tool the programme has. The same was true one level up: the AM
  review records its probes as sentences, the AN pass re-derived mutations from those sentences, and
  this pass re-derived them again — and **four could not be set up at all**, because the text they
  anchored on had been corrected by the AN pass and nothing said so. "The guards fire" is a claim both
  retained reviews make and nothing was checking. `conformance/channel-0.2-guard-probes.json` now
  holds the 45 probes the three passes validated and `build/verify-channel-0.2-guards.ps1` runs them
  in the repository gate, failing on a probe whose anchor has moved rather than skipping it.

**What generalises is the direction of AO1's fix and it is now this work's most repeated lesson.** A
guard that silently drops what it cannot read certifies its own completeness. AM1's status region
answered that with a permit list, AN1's pointer check with resolution, AN2's pin with one derived list,
and AO1's row reader with a parser that fails on an unrecognised cell. Four cycles, one shape, and the
answer has been the same every time: **make the unreadable case loud.**

**One question this pass does not settle**, recorded here rather than decided: whether the guard corpus
should become a fifth work item with its own acceptance, or stay what it is — a corpus retained beside
the gates and grown by whichever pass adds a guard. It is in the repository gate either way. Section 5
carries it as open question 4.

## 3. How the hold ends

The cycle resumes when, in this order:

1. W1 is done for the three frame references and the `unseen` refusal record, and the registry check
   they motivated is deleted rather than extended;
2. W2 runs `C4-P1`, `C4-P2`, `S1`-`S6` and `I1`-`I7` executably in the gate, with every required-green
   set stated rather than `owed`;
3. W3 has shrunk the status blocks; and
4. an author-side iteration pass over W1-W3 finds nothing it can fix — which under the
   [two kinds of review](./reviews/README.md#two-kinds-of-review) means the work is ready to be
   reviewed, not that it passed.

Then one fresh independent closure review is dispatched under the unchanged independence rules.

**Conditions 1, 2 and 3 are met**, each as its own section above records. **Condition 4 has run three
times and is met by none of them**: the passes found three, six and three defects and fixed them all,
which is the opposite of what that condition asks. Sections 2d, 2e and 2f record them.

The three are not the same pass repeated, and the trend is the argument for running a fourth rather
than for giving up on the condition. AM recomputed numbers; AN asked where else each corrected fact was
stated; AO read each guard's comment as a claim and tested it against the code. Each brief came from
the pass before it, each found something the previous method could not have, and **AO1 — two properties
red on conforming behaviour — is a defect a closure reviewer would have been entitled to call
blocking.** The condition is doing what it was written to do; it has not yet run out of findings.

The next work is therefore a fourth author-side pass over the same scope -- the declarations, the
four gates, and what the nine design artifacts now say where a rendered fact replaced prose, not a
re-reading of the design the sixteen closure reviews have been reading. It starts by running
`build/verify-channel-0.2-guards.ps1`, which is the probe corpus AO3 retained so that no pass rebuilds
it again, and it meets condition 4 only if it finds nothing it can fix. The work AO left is named in
its review: the design verifier's seventy-eight claim blocks, of which the totality and derivation
claims were tested and the narrative ones were not. Nothing in this section authorizes dispatching a closure review,
and the closure-cycle state at the head of this document is what says so.

## 4. What to measure

Recorded so the next decision is made on evidence rather than on how the cycle felt:

- **findings per cycle, split by class** — propagation slip, unsound property, stale index, other. The
  claim behind this plan is that the first and third classes go to zero and stay there;
- **surfaces per fact** — should fall to one plus citations. It has not, and deliberately: the
  implementation renders rather than cites, so the four owned facts stand at **21 publication sites**
  and no hand writes any of them. The measure this replaces it with is how many of a fact's surfaces
  are hand-maintained, which is **zero** for the three frame references and the `unseen` refusal
  record and was every surface of all four when this plan was written;
- **properties executable in the gate** — currently **twenty-six of twenty-six**. Every capability-wide
  property the package declares runs on every commit;
- **required-green sets stated** — currently **twenty-six of twenty-six**, having been one for seven cycles.
  No cell in the completeness review's two property tables reads `owed`;
- **status-block lines across the nine artifacts** — **265** at `9ce01a0` and **45** now, both
  recomputed by the design verifier rather than read;
- **Channel index row characters** — **8,746** at `2684ec7` and **1,318** now, summed over the eleven
  per-artifact state cells and recomputed by the design verifier. This measure said 1,208 for three
  commits, which was never the value at any commit; it is corrected under **AM3**. It has moved twice
  since, by four characters each time and for the same reason — registering a new iteration-review
  family in the Design reviews row — and on both occasions the check that recomputes it failed the
  figure on the commit that wrote it, which is the check working rather than a defect in it; and
- **guard probes executable** — currently **45 of 45**, run by
  `build/verify-channel-0.2-guards.ps1` in the repository gate and recomputed by it. This measure did
  not exist before **AO3**, and what it is for is the claim "the guards fire", which three passes
  asserted in prose while four of the probes behind it had quietly stopped applying; and
- **design-verifier lines** — **2,614** now, recomputed by the verifier against itself. Every step
  of this work, each figure recomputed from the repository rather than stated: `6c7715a` **2,322** when
  the work began, `365bbc0` **2,377**, `2684ec7` **2,257**, `72fecde` **2,263**, `46b7c85` **2,247**,
  `0f7858c` **2,356**, `6a6c76d` **2,441**, `c5fe9ee` **2,491** — counted the way this verifier counts
  its own lines, where a line break is CRLF, LF or a lone CR. That reader is named because it matters
  once: the file carried two stray carriage returns at `0f7858c`, so `wc -l` reports 2,354 there and
  agrees everywhere else. It is **AM4**'s lesson in a second guise — a verifier that reads history
  reads through a reader nobody chose — and it was found by writing the check rather than by reading.
  **The measure has risen over the work as a whole and it is stated rather than smoothed over**: it
  fell twice, when the frame-reference registry was deleted and when the AL2 sweep moved out to the
  facts gate, and rose at every other step. This bullet said it "fell at each step" and put three of
  its four deltas at numbers no reading of the history produces, which is **AN3** — the third measure
  left to prose in a section whose other two were corrected for being left to prose. What the rises
  bought is the trade this plan argues for: most of them are checks that compute a number this
  document used to assert, or a guard replacing a hand-written list, and whether that stays worth it is
  the question this measure exists to keep visible. Two further gates sit beside it -
  `verify-channel-0.2-properties.ps1` and `verify-channel-0.2-facts.ps1` — so the total verification
  code in the repository has grown throughout; what this measure is for is whether the DESIGN verifier
  is still absorbing the cost of a structural problem.

## 5. Open questions for the owner

1. **Does standalone readability of each artifact survive W1?** If an artifact must remain readable
   alone in the strong sense — no link-following — then W1 is not available and the recurring family
   has to be managed some other way. This is the question that decides whether the plan's first item
   is real.
2. **Is a hand-written input set acceptable for W2 before Batch 2 authors vectors?** The alternative is
   waiting for Batch 2, which is gated on the closure this plan exists to unblock.
3. **Should the twenty-five owed required-green sets be filled inside W2, or as their own pass first?**
   Filling them will surface property defects in a batch — as the `AK` enumeration did — and that is
   the point rather than a risk.
4. **Is the guard corpus a fifth work item, or a standing practice?** **AO3** retained it and put it in
   the repository gate, which settles that it runs. What it does not settle is whether adding a probe
   with each new guard is an acceptance criterion someone can fail, as W1's registry deletion was, or a
   habit that decays the first time a pass is in a hurry. The measure in section 4 makes the decay
   visible either way, which is the argument for leaving it as a practice.
