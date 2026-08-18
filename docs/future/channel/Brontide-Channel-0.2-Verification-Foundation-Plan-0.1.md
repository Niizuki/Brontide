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
run in the gate on every commit: 55 evaluations over 29 declared inputs, plus the nine operand
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

**What W2 still owes.** The eleven `C1`-`C12` per-capability properties other than C4 are still prose
and still owe their required-green sets. They are outside condition 2 and are the remaining eleven of
twenty-six.

## 2b. What W3 has landed

**The nine status blocks carry no disposition history.** Each states what the artifact is and what it
awaits, in five lines, and links to its section of the
[disposition index](./reviews/channel-0.2-disposition-index.md) -- a retained review record, not a
design artifact. Between them those blocks held **289 lines** and now hold **45**. Nothing was
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
dies for the maintainer. The fact is owned by `conformance/channel-0.2-facts.json` and **rendered**
into all twenty publication sites, each a fenced region the artifact carries:

    <!-- fact:NAME -->its kind, its **session**, ...<!-- END -->

with `NAME` the fact's id and the closing marker `/fact`. The markers are written as placeholders
here because a real pair in this document would be a real publication, and the gate says so --
which is how this paragraph was caught on its first run.

A reader of the grid alone still learns what the `unseen` cells record, in the same words as before.
No human writes the second copy. Changing a field is one edit to the declaration and
`build/verify-channel-0.2-facts.ps1 -Apply`, which rewrites all twenty sites in five artifacts; this
was tested by adding a sixth field, applying it, and reverting.

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

**What W1 still owes.** This covers the three frame references, which is what condition 1 below
requires. The per-session facts C12 declares and the settling-frame and terminal-frame field lists in
the completeness review's operand enumeration are stated once each and not yet fenced; the `unseen`
refusal record's *other* contents -- provenance, detailed reason, effect certainty -- are still
hand-maintained prose, and the AL2 record-keyed sweep is what watches them.

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

## 4. What to measure

Recorded so the next decision is made on evidence rather than on how the cycle felt:

- **findings per cycle, split by class** — propagation slip, unsound property, stale index, other. The
  claim behind this plan is that the first and third classes go to zero and stay there;
- **surfaces per fact** — should fall to one plus citations;
- **properties executable in the gate** — currently fifteen of twenty-six: `C4-P1`, `C4-P2`,
  `S1`-`S6` and `I1`-`I7`, which is every property the hold's second condition names. The
  remaining eleven are the `C1`-`C12` per-capability properties other than C4;
- **required-green sets stated** — currently fifteen of twenty-six, having been one for seven
  cycles. The fourteen filled here are the fourteen the second condition names;
- **status-block lines across the nine artifacts** — 289 before W3 and 45 after;
- **Channel index row characters** — 8,746 before W3 and 1,208 after; and
- **design-verifier lines** — **2,263**, down from 2,322 when this work began. It has fallen for the
  first time. W1 took 169 lines out with the frame-reference registry and W3 took 32 more with the
  index-row freshness checks, against additions for the checks that replaced them. Two new gates sit
  beside it — `verify-channel-0.2-properties.ps1` and `verify-channel-0.2-facts.ps1` — so the total
  verification code in the repository grew; what this measure is for is whether the DESIGN verifier
  is still absorbing the cost of a structural problem, and on that it has started to shrink.

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
