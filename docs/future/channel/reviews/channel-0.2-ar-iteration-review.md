# Channel 0.2 sixth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-sixth-pass-2026-08-21-a5ec7a5`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), W3 (the status blocks and the
Channel index rows), the guard corpus AO3 retained, and the AQ corrections — at `a5ec7a5`,
`Merge pull request #136`; raised and dispositioned the AR1 finding this document records

Date: 2026-08-21

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the **sixth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it either**, on one finding.

## Method

It ran the corpus first: **61 of 61 in one command.** Then it did the two things the AQ review left,
in the order that review put them in.

**It retained the instrument.** The AQ pass built a coverage trace, found four of its five findings
with it, and kept only the output — which is section 1.1 of the plan happening for the third time,
after the discarded property evaluators and the probes rebuilt from prose. It is now
`build/verify-channel-0.2-coverage.ps1`, it runs in the repository gate, and its rule is that **every
conditional in a covered gate must be evaluated by a passing run.** Constructs a passing run correctly
cannot reach are declared, with their reasons, in `conformance/channel-0.2-coverage-exemptions.json`;
an entry whose construct becomes reachable fails, and an entry whose construct disappears fails as
stale, on the rule the probe corpus already applies to a rotted anchor.

Three things about it are worth recording, because each was a choice that could have gone wrong:

1. **The unit is a condition, not a statement.** A passing gate is *supposed* to skip its failure
   bodies, so a measure over statements reports every check in the file. The first draft measured
   statements, reported 179 across the three gates, and would have been abandoned as noise inside a
   cycle. A measure over conditions reports only the checks whose condition was never reached.
2. **Two exemption classes are structural rather than declared** — anything inside a `catch`, and the
   `foreach ($failure in $failures)` loop each gate prints with. Both are expressed as a walk to the
   root and as the collection the loop walks, because **AN2** was a list that held eight of nine.
3. **It refuses a dirty tree.** Several checks are guarded on the repository's committed state — the
   review-target pin skips itself outright while a design artifact has uncommitted edits, correctly —
   so coverage measured mid-edit reports those as checks that never run. That was observed, not
   predicted: the gate reported the pin block on its first run against a working tree. A measure that
   cries wolf while someone is working is a measure someone writes an exemption for, and an exemption
   is permanent where the dirty tree was not.

**Then it hunted what the trace cannot see** — the AQ5 brief, a negative assertion whose extent nothing
declares. That hunt produced no finding. It is recorded below as unswept rather than as clean.

## Findings

### AR1 — two property clauses that no declared input reaches — corrected

The coverage gate's first run reported two conditions never evaluated across 113 evaluations over 41
declared inputs, and both are in the design's subject rather than in the gates.

`C5-P1` and `C6-P1` **each state two clauses**:

> **`C5-P1`.** Every dispatched vector has passed every declared bound and every positional Shape
> rule; **every pre-dispatch structural refusal records `known-none`** and no semantic Outcome.

> **`C6-P1`.** No C6 vector reaches handler dispatch unless one exact local authority decision is
> `permitted`; **every denial or unevaluatable presentation records the decision point, initiator
> attribution, and `known-none`**.

Each had one named mutation, and **each mutation fires through the first clause.** For `C5-P1` no
declared vector carries a pre-dispatch structural refusal at all, so nothing in the corpus can reach
the second clause. For `C6-P1` the corpus carries exactly one non-permitted authority decision and
that interaction is also dispatched, so the first clause returns before the second is reached.

Pinned before it was corrected: **both second clauses were deleted outright from the evaluator and the
properties gate passed** — 26 of 26 properties executable, 113 evaluations, 9 operand mutations, green.

**This is a rule the design already states.** The completeness review's `C4` audit row says it in the
course of explaining C4's own pair: *one named mutation per conjunct, because half a property with no
mutation is half unfalsifiable.* It was enforced for `C4-P2`, the one property that declared conjuncts,
and silent for the other twenty-five — which is **AP2**'s shape, a rule enforced over the surfaces one
audit happens to enumerate, and **AE3**'s, a clause that cannot fail. No owner ruling is required, for
the same reason **U1** needed none: the correction restores what the design already claims rather than
choosing between defensible designs.

**Corrected** by naming each property's two clauses in the evaluator, giving each clause its own named
mutation — `C5-pre-dispatch-refusal-possible-effect` and `C6-denial-without-decision-point` — and
registering both in the per-capability property audit, which is the artifact Batch 2 authors property
files from and which had described both properties **by their first clause alone**. The existing
requirement that a mutation declared against a conjunct must fire *through* that conjunct now reaches
them.

**The class is closed rather than the two instances.** A clause that no input reaches is now a gate
failure, in every property, on every commit.

## What this pass verified rather than believed

- **The retained corpus runs clean**: 61 of 61 before this pass's work, in one command.
- **AR1 was pinned before it was fixed**, by deleting both clauses and watching the gate pass.
- **The coverage gate was watched failing for the right reason and then watched going green.** It
  reported exactly the two clauses and nothing else, and after the two mutation vectors landed it
  reported the pin block — which is the dirty-tree effect above, diagnosed rather than exempted.
- **A finding this pass thought it had is recorded as withdrawn**, because it matters more than the
  ones that held. Every vector carries an `expected` map restating the verdict the property
  declaration owns, and a grep for the field's use in the gate found nothing, which read as 41 vectors
  of unchecked second surface — the exact class W1 exists to end. It was **probed before it was
  written up**: one vector's expectation flipped to the wrong verdict, and the gate failed with a
  message naming the vector and both verdicts. The check is there, in both directions, and the grep
  was wrong. Under this programme's own rule — where a probe and an artifact disagree, the artifact is
  right — the finding was the reading, not the file.

## What this pass did not do

**It did not sweep the AQ5 class, and this is the most important sentence in this document.** The
brief was to hunt an assertion whose extent nothing declares, beyond the two windows the AQ pass
corrected. That hunt was attempted and produced nothing, and *produced nothing* is not the same as
*there is nothing*: the coverage instrument cannot see this class by construction — it finds a check
that never runs, and an under-reaching assertion runs perfectly well — so the search was a reading,
which is the instrument four passes have now shown to be the weak one. **The class is unswept, not
clean.**

It did not extend coverage to the guard corpus harness or to this new gate itself. Both are gates by
the same argument, and neither is measured.

It did not measure branch coverage inside a condition. A conditional with three operands is evaluated
if any one path reaches it, so a compound condition can hide an operand no input exercises — which is
the same shape as AR1 one level down.

It leaves **condition 4 open**, on one finding: three, six, three, two, five, **one**. The number is
smaller and the reason is not that the work is nearly clean. The pass spent most of its effort
building an instrument rather than hunting, and the one thing it hunted by reading it did not find.

## Where this family is dispositioned

**AR is raised against the design**, unlike AM through AQ. The finding is about which inputs the
property gate runs, but its correction reached the completeness review's per-capability property audit,
and under the owner ruling of 2026-08-20 a family whose correction reaches a design artifact is a
design family whatever its author called it. Its disposition therefore lives in the
[completeness review's disposition history](../Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md),
and the [provenance table](./README.md#finding-family-provenance) declares it on both axes.

The coverage gate itself is verification work and is recorded in
[section 2i of the verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md),
which is where the AQ review's owed item is discharged. It raised no finding of its own; it is the
instrument AR1 was found with.
