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
- **properties executable in the gate** — currently zero of twenty-six;
- **required-green sets stated** — currently one of twenty-six, unchanged for seven cycles; and
- **design-verifier lines** — currently over two thousand. If W1 and W3 work, this falls. A verifier
  that only grows is a verifier absorbing the cost of a structural problem instead of retiring it.

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
