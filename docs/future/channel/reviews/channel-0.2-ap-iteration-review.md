# Channel 0.2 fourth W1-W3 verification-foundation iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-condition-4-fourth-pass-2026-08-20-108f0c9`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their two gates), W2 (the twenty-six executable properties), W3 (the status blocks and the
Channel index rows), and the guard corpus AO3 retained — at `108f0c9`,
`Merge pull request #134`; raised and dispositioned the AP1-AP2 findings this document records

Date: 2026-08-20

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires.

It is the **fourth** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it either**, and it found two findings rather than the three, six
and three of the passes before it.

## Method

**It began by running the probes instead of rebuilding them, which is what AO3 was for.** One command,
45 of 45 green, in the time the three previous passes each spent reconstructing mutations from the
prose of the pass before. That is the whole of the first step now, and it is worth recording as the
first measurable return this verification work has produced.

The rest is the brief AO left: **finish the design verifier's claim blocks.** AO tested the totality
and derivation claims among its seventy-eight and left the narrative ones. This pass read all of them
and probed three shapes the earlier passes had not:

1. **Blocks gated on a phrase.** Three conditionals in the design verifier wrap several checks each in
   `if (<artifact> contains <phrase>)`. Each one is a claim that the phrase cannot be removed without
   the obligation going with it. That claim was tested for all three.
2. **Checks that name members of a class they claim to be total over.** A hardcoded id list inside a
   check whose comment says "every".
3. **Counts derived versus counts asserted**, in the checks that report package-wide numbers.

Shape 1 gave **AP1**, shape 2 gave **AP2**, and shape 3 found the AK3 counts correctly derived from
the artifacts.

## Findings

### AP1 — one sentence could silence twenty-four checks, and the reason it could not has expired — corrected

The largest conditional block in the design verifier — the U1, AC3, V2, W1, W3 and W4 checks, twenty-four
failure sites — is keyed to C4's assertion that `C4-control-precedes-request` is the mutation `C4-P2`
must go red on. The comment states why that key is safe:

> It is keyed off the claim that *depends* on falsifiability [...] rather than off the property's own
> wording, so deleting the claim cannot make the check pass while leaving an untestable promise
> standing.

That was true when it was written, and **W2 ended it.** The promise now lives in
`conformance/channel-0.2-properties.json` and executes in the properties gate, so deleting the
sentence leaves the promise standing perfectly well and silences the block. Probed at the parent
commit: the sentence removed, twenty-four checks skipped, **both gates green.**

The other two blocks of the same shape were probed and are sound — reword the `unseen` retention
sentence or the parity list's settling-frame phrase and other checks fail, because both phrases are
separately required elsewhere. AP1 is the one whose key nothing else holds down.

**Corrected** by keying the block to `C4-P2`'s own existence, which cannot be deleted quietly — the
properties gate fails when the declaration names a property its stating artifact does not carry — and
making the falsifiability sentence the **first thing checked** rather than the thing that decides
whether anything is checked. An absent claim is loud instead of silencing, which is where AM1, AN1,
AN2 and AO1 each ended.

### AP2 — the audit-coverage check samples four of the twenty-six properties it says it covers — corrected

The AF7 check requires the completeness review's per-capability property audit to carry a row for
`S1`, `S6`, `I1` and `I7`. Its own comment is about totality, and about the failure mode of not having
it:

> C12's rule is written over "every property" [...] AE4's mechanism -- a rule enforced over the
> surfaces one audit happens to enumerate.

Four ids is 15% of the twenty-six properties the package states. Probed: a row that keeps its text and
loses its property id passes both gates for the other twenty-two. Deleting a whole row is caught, but
only incidentally — the properties gate cites the row's *text* for the property's required-green
member, so the row's identity as that property's registration is unguarded.

The audit is the register of property/mutation pairs and the artifact Batch 2 authors property files
from. A property silently dropping out of it is the design ceasing to claim it audited that property.

**Corrected** by moving the enforcement to where the set of properties is known. The properties gate
now requires every declared property to be registered by a row carrying its id — as a row key for the
session and interaction properties, or named inside its capability's row for the twelve capability
properties — over the **declared set** rather than a list. The sampled check is deleted rather than
extended, because a sample beside a total check is the second enumeration AN2 was.

## What this pass verified rather than believed

- **The retained corpus runs clean**: 45 of 45 before this pass's work, in one command.
- **It caught its own staleness during the pass.** Changing the design-verifier line measure twice
  moved the text one probe anchored on, and the corpus failed on the commit that did it rather than a
  cycle later. The probe is re-anchored to the stated form rather than to the number, which is the
  more durable anchor and the lesson for the next probe someone adds.
- **The other two phrase-keyed blocks are sound**, probed in both directions.
- **The AK3 counts are derived** from the property statements themselves, not asserted.
- **The attestation ordering rule holds**: an entry filed out of sequence fails, as AJ7 requires.
- **The iteration-review disclaimer rule holds**: a review that drops it fails.
- Eight probes were added to the corpus for all of the above, which is the practice AO3's open
  question asks about — a pass that adds a guard adds its probe.

## What this pass did not do

It did not re-read the design; condition 4 scopes it to the verification work.

It did not probe every one of the seventy-eight claim blocks individually. All were read; the three
structural shapes above were probed exhaustively within their shape. The blocks left untested are
single-check phrase assertions of the form "artifact X must state Y", where the check is the claim
restated and there is nothing between the two for a probe to find.

It leaves **condition 4 open**, and the count is falling — three, six, three, two. What the next pass
inherits is narrower than what this one did: run the corpus first, and then take the one shape this
pass found and did not exhaust, which is a guard whose key was correct when written and stopped being
correct when the work moved. AP1 is that shape and W2 is what moved. **Every check written before W2
and W3 has the same question outstanding: is its key still load-bearing?** Three of them were probed
here. The rest were read.

## Where this family is dispositioned

AP is raised against the verification work rather than against the design, as AM, AN and AO were, so
under the owner ruling of 2026-08-20 its disposition lives in
[section 2g of the verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md)
and the [provenance table](./README.md#finding-family-provenance) declares it on both axes. No AP
finding reaches a design artifact.
