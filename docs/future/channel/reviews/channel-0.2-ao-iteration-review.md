# Channel 0.2 third W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-third-pass-2026-08-20-d01e706`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), and W3 (the status blocks and
the Channel index rows) — at `d01e706`, `Merge pull request #133`; raised and dispositioned the
AO1-AO3 findings this document records

Date: 2026-08-20

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the **third** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names, after [AM](./channel-0.2-am-iteration-review.md) raised five and
[AN](./channel-0.2-an-iteration-review.md) raised six. **Condition 4 is not met by it either**, and
one of its three findings is the most serious the verification work has produced: two capability
properties were red on conforming behaviour.

## Method

The AN review handed this pass a specific brief, and it is what produced every finding here:

> two of the six were guards whose comment stated a stronger question than the code asked; reading
> each guard's comment as a claim to be tested against its code is a cheap pass nobody here has run in
> full.

So the method was: re-run the recorded probes first, then read every comment in the three gates that
makes a **testable structural claim** — a totality, a both-directions, a derivation, a "must" — and
test that claim against the code beneath it. 103 such comment blocks across the three gates.
**AO1** came out of the fifteenth of them.

The other half of the brief — asking *where else is this stated* mechanically rather than by reading —
found nothing new in the maintained records, which is the AN corrections holding.

## Findings

### AO1 — `S1` and `C2-P1` are red on a session that faults, and the guard that exists to prevent that cannot see the rows that make it legal — corrected

**This is the AE1 defect: a property that cannot stay green on conforming behaviour.** It took the
programme ten cycles to find the first instance and this is the fifth, in a gate written after the
lesson.

`S1` is evaluated against a copy of the session machine's legal transition table held in
`build/verify-channel-0.2-properties.ps1`. The copy is a second surface for a design fact, which the
file knows, so a cross-check compares it against the artifact in both directions. Its comment states
the claim exactly:

> every edge declared above must appear as a row of that artifact's own transition table, and the
> artifact must declare no accepted edge this file does not carry. A row added there and forgotten
> here would make `S1` red on conforming behaviour.

The artifact's table has **ten** rows. Eight name a state in the From cell. The last two say
**`any nonterminal`** — a fatal recognized Channel violation, and a transport or process loss — and
both go to `faulted`. The row reader required a backticked lowercase state in that cell, so it saw
eight rows, compared eight against eight, and reported the two lists identical. The three edges those
wildcard rows add and the list did not carry are `unestablished>faulted`, `establishing>faulted` and
**`established>faulted`**.

Every column of the coverage grid's `established` row routes to `faulted`, so this is not an edge case
in the design — it is what the design says happens when a session faults. Probed by adding the
transition to a conforming vector:

```
FAIL: Property 'S1' is red on 'S-conforming-single-session' (required-green) and must be green.
      Witness: session s1 accepted the transition established>faulted on event fatal-protocol-fault,
      which the legal table does not contain.
FAIL: Property 'C2-P1' is red on 'S-conforming-single-session' (required-green) and must be green.
```

**Corrected in three parts.** The From cell is **parsed** rather than matched — a backticked state, or
the class `any nonterminal` expanded over the states the machine's own state table declares
nonterminal — and a cell the parser does not recognise is a **failure rather than a row it drops**.
That direction is AM1's permit list: a guard that silently drops what it cannot read certifies its own
completeness. The list gains the three edges. And the vector
`S-conforming-fault-from-established` is retained as an additional-green member of all
twenty-five properties the conforming single-session vector belongs to, so the false red is pinned by
an input and not only by a comparison of two lists.

Turning the same question on the correction found one more thing, which is recorded rather than
raised separately: the expansion needs to know which states are terminal, and the first draft read
that from a list in the verifier — AN2's second enumeration, arriving inside the fix for AO1. It now
reads the artifact's own Terminal column, and the copy the file does keep is checked against it.

### AO2 — section 2a's account of what this gate runs is four prose numbers, and one added vector moved all four — corrected

Adding the AO1 vector changed the evaluation and input counts in both of section 2a's sentences. The
plan's section 4 measures are recomputed by the gates after AM2, AM3 and AN3; section 2a's are the
same kind of number about the same runs and were never included.

**Corrected**: both sentences are stated in a form the properties gate recomputes, against what it
actually ran — 69 evaluations over 30 inputs for the fifteen properties condition 2 names, and 113
over 41 for all twenty-six. The set of "the fifteen" is taken from the hold's own list rather than
from a number, so a property joining that condition joins the measure with it.

### AO3 — the probes are prose, and three passes have rebuilt them by hand; four of them had rotted unnoticed — corrected

Section 1.1 of the plan is about evaluators:

> Every reviewer **writes an evaluator from the published prose, uses it, and throws it away** —
> reviews 8, 9, 12, 15 and 16 all did. That is the single most productive tool this programme has and
> it has never been kept.

The same was true one level up, of the probes that assert the guards fire. The AM review records its
probes as sentences. The AN pass re-derived mutations from those sentences. This pass re-derived them
a third time — and **four could not be set up at all**, because the text they anchor on had been
corrected by the AN pass and nothing had said so. A probe nobody can run is a claim nobody is
checking, and "the guards fire" is a claim both retained reviews make.

**Corrected by keeping the instrument.** `conformance/channel-0.2-guard-probes.json` holds 45 probes —
every mutation the AM, AN and AO passes validated — and `build/verify-channel-0.2-guards.ps1` runs
them in the repository gate: it applies each mutation, runs the gate that owns the guard, and requires
the verdict that guard owes. A probe whose anchor has moved **fails** rather than being skipped, which
is the condition that went unnoticed for a cycle.

Two boundaries are stated in the file rather than left implicit. A probe is evidence about a *guard*,
never a statement about the design: where a probe and an artifact disagree about what the design says,
the artifact is right and the probe is the defect. And a guard with no probe is not thereby sound — it
is unmeasured, which is the state this file reduces rather than ends.

The runner mutates the working tree, so it restores from bytes read before each mutation, refuses to
run over a path with uncommitted changes, and verifies at the end that it left nothing behind. All
three rules are paid for: restoring with `git checkout --` discarded an hour of uncommitted
corrections during the AN pass, on the setup-failure path where the probe never ran.

## What this pass verified rather than believed

- **Every probe the AM and AN reviews record was re-run.** All of them reproduce, once re-anchored,
  and they are now the corpus AO3 retains.
- **The `-Apply` claims hold.** Adding a field to one fact's declaration and applying it rewrites
  exactly that fact's sites — 6 of the 21, across 5 artifacts — leaves every other fact's fences byte
  identical, preserves each file's own line structure, and leaves the gate green. Adding a field to
  all three frame references rewrites all 21 sites across 6 artifacts.
- **The facts gate's own guards fire**: an inconsistent declared rendering, a field name outside the
  class assertion, an unbalanced fence, an undeclared publishing artifact, and a publication count
  that disagrees with the declaration each fail.
- **The AN corrections hold under a mechanical sweep** for restated repository counts across the
  maintained records.
- **The session-scope audit is not the same shape as AO1**: the property parser asserts it read as
  many statements as the package states, so a property statement it cannot read is a failure rather
  than a silent omission. That is the guard AO1 lacked, in the file next to it.

## What this pass did not do

It did not re-read the design; condition 4 scopes it to the verification work. AO1 was found by
reading a *verifier's* comment against its code, and the artifact it turned out to disagree with was
read only to settle which of the two was wrong. The design is right and the gate was wrong.

It did not audit every one of the 103 claim blocks to the same depth. The facts gate's fifteen and the
properties gate's ten were read in full; of the design verifier's seventy-eight, the totality and
derivation claims were tested and the narrative ones were not.

It leaves **condition 4 open**. Three findings is not "nothing it can fix". The pass that runs next
should begin by running `build/verify-channel-0.2-guards.ps1` rather than by rebuilding it, which is
what AO3 exists for, and then finish the design verifier's claim blocks — that is where the remaining
untested comments are, and it is where AO1's shape would live if there is another one.

## Where this family is dispositioned

AO is raised against the verification work rather than against the design, as AM and AN were, so under
the owner ruling of 2026-08-20 its disposition lives in
[section 2f of the verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md)
and the [provenance table](./README.md#finding-family-provenance) declares it on both axes. No AO
finding reaches a design artifact: AO1's defect was in a gate's copy of a design fact, and the design
fact itself is unchanged and was correct throughout.
