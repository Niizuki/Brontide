# Channel 0.2 AD correction iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-ad-correction-iteration-2026-08-14-4a52a56`

Reviewed work: the correction sequence at branch head `4a52a56`,
`fix(channel): close AC1-AC4, the layer under the Y and V corrections`, and the U-through-AC families
beneath it; raised and dispositioned the AD1-AD3 findings this document records

Date: 2026-08-14

**This is an iteration review, not an attestation.** It ran in the working repository rather than in a
fresh isolated clone, and its actor corrected what it found. Under
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires. It exists
so a fresh reviewer does not spend its one cold pass on defects that were already visible.

## Method

Two passes. The first followed the handoff — evaluate `C4-P2` rather than read it — and found nothing:
an evaluator built from the contract's prose puts both conjuncts red on their named mutations
(`C4-control-precedes-request`, `C4-outcome-precedes-ack`) and green on both nonconformant-peer cases,
the duplicate terminal separating from a reordering only because Y4's arrival ordinal is present. AC1
and AC2 are carried by every artifact that reads them. That is real evidence for the U-through-AC line.

The second pass asked a different question, and it is the one that produced these findings: **the AC
pass audited artifacts against their descriptions; what happens if the descriptions are audited
against the artifacts?** Three documents describe what the W correction iteration review contains.
All three disagree with it, and one of them denies it outright.

## Findings

### AD1 — a retained review denies a record that exists, and refers the gap to the owner — corrected

The AC review's residual section stated that the AA and AB passes had left no retained record of their
own, that this was the X7 gap reappearing, and that the owner must choose between reconstructing
the reasoning and rescoping the requirement. None of that holds. The
[W correction iteration review](./channel-0.2-w-correction-iteration-review.md) records AA1-AA3 under
`## Fourth pass` and AB1-AB2 under `## Fifth pass`, each with the mechanism-and-disposition treatment
its X, Y, and Z findings get.

The cost is not bookkeeping. A residual that names an owner decision is a live instruction to the next
reader, and acting on it would have meant authoring duplicate records for passes that already have
them — or rescoping a requirement that was never violated. This is the first finding in the sequence
where the defect is in the *evidence about* the design rather than in the design.

How it happened is worth more than the finding. The AC pass read the W review's roster entry and its
scope line, both of which stopped short of AA and AB, and never opened the document. That is AC1 in
miniature — auditing an artifact's description rather than the artifact — committed by the pass that
raised AC1, one section below where it raised it.

**Corrected.** The residual is corrected in place rather than deleted, because the retraction is worth
more as a record than the original claim was, and a deleted residual leaves the next reader no way to
know the question was settled. AC4's closing sentence, which rested on the same false premise, is
corrected with it.

### AD2 — the second half of the X7 class check is written over two ids — not corrected

The X7 comment in the design verifier states the check is written over the general class rather than
over the six W ids, and names two halves: every finding a retained iteration review raises must appear
in the disposition history, **and** a finding family the review policy attributes to an iteration pass
must have a retained record. AC4 corrected the first half by widening its family pattern to two
letters. The second half is `foreach ($findingFamily in @('W1', 'W6'))`.

Probed rather than asserted: the review policy bolds 36 finding ids, and the check evaluates 2. There
is no live gap — every family does have a retained record, which the AD1 correction is what confirms —
so this is latent, not a present defect. But the check cannot fail for AA, AB, AC, AD, or anything
after them, which is the property AGENTS.md asks of a guard: it should fail when the next member is
added and left out, rather than assert today's members by name.

**Not corrected**, deliberately. AC4 has just been through this check and read the same comment, and a
second actor widening the same block in the same week is worth less than a fresh reviewer deciding
whether the hardcoded pair is a defect or a deliberate narrowing that the comment describes badly. It
is recorded here so that decision is made rather than inherited.

### AD3 — three accounts of one retained review, none of them matching it — corrected

The W review records X, Y, Z, AA, and AB families. Its own scope line enumerated through AA1-AA3 and
omitted AB. The review policy's roster entry described it as raising X1-X7, then Y1-Y4 and Z1-Z4, and
stopped there. The AC review said the AA and AB records did not exist at all, which is AD1.

A partial enumeration is read as the whole of it, and each of these three is what some later pass used
instead of opening the document — AD1 is the proof that at least one did.

**Corrected**: the scope line and the roster entry name every family the document records, and the
roster entry says plainly that the AA and AB families live in its fourth and fifth passes rather than
in a separate file.

## Checks

Written first and observed failing against the pre-correction artifacts, with the failure message
naming the mechanism claimed for it in each case. Five failures were observed across AD1 and AD3.

The check is written over the class rather than over these three statements: each retained iteration
review's families are derived from its own `###` finding headings, and three rules are asserted
against that set.

| Check | Pins | Mutation reverted |
| --- | --- | --- |
| roster entry names every family its review records | AD3 | drop `AA1-AA3, and AB1-AB2` from the W entry |
| a scope line that enumerates families enumerates all of them | AD3 | drop `AB1-AB2` from the W scope line |
| no retained review denies a record that exists | AD1 | restore the AC residual's original claim |

Each was mutation-tested after correction by reverting the edit it pins and confirming it fires again,
with the counts and messages above.

Two weaknesses in these checks were found by running them and are recorded because they bound what the
checks are worth. The roster section was initially read to end of file, which made the
disclosed-deviation sections' links to the same files read as roster entries; it is now bounded at the
next heading. And requiring every scope line to enumerate its families imposed the W review's
convention on two documents that legitimately use another — naming the corrections reviewed rather
than the findings raised — so the rule now fires only on a *partial* enumeration, which is the actual
defect.

The denial check reads assertion and quotation alike. A later pass retracting such a claim must not
restate it verbatim beside the families it names; that is a constraint on how a retraction is worded,
and the alternative is parsing negation, which would fail open on the assertions the check exists to
catch. This document's own AD1 section is written under that constraint.

**This evidence is worth what the W and AC reviews said theirs was worth, and for the same reason.**
The checks were written by the actor that wrote the corrections, from one reading of the same prose.
Every blocking finding in this programme was raised against artifacts whose gates were green — these
included, since AD1 sat behind a green gate through the entire AC pass.

## Gates

`build/verify-channel-0.2-design.ps1`, `-NegativeProbe` (fails only on the in-memory `C12-P1`
removal), `build/verify-doc-links.ps1`, `build/verify-text.ps1`, and `build/verify-interchange.ps1`
all pass at the corrected commit.

## Residual, not corrected

**AD2 is open and is an owner call**, for the reason its finding states.

**The two questions the X, Y, and AC passes did not settle remain open** and are unchanged by this
pass: no artifact bounds the volume of the observation record `C4-P2` now depends on, and the arrival
ordinal is still an observation-local counter with no statement about whether a frame refused before
correlation consumes one.

**What this pass did not do.** It did not re-derive the grid enumeration, did not re-verify the
retained findings through S1-S3 individually, and read the review policy in full before assessing
anything — which is why it is an iteration review and not a closure attestation. A fresh reviewer's
cold context is still the only thing that can close the batch, and nothing here should be taken as
evidence that it will.
