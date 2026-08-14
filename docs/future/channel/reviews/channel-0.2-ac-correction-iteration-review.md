# Channel 0.2 AC correction iteration review

Date: 2026-08-14

Reviewed: the correction sequence from `fix(channel): make C4-P2 falsifiable` through
`docs(channel): tighten the C4 retention passage`, at branch head `35b371d`.

This is an iteration review, not an attestation. It was performed in the working repository rather
than in a fresh isolated clone, by an actor that then corrected what it found. Under the
[two kinds of review](./README.md#two-kinds-of-review) that makes it legitimate author-side work and
nothing more: it does not close the first batch, does not authorize Batch 2, does not produce the
closure record, and its verdict is not the conforming verdict the Closure section requires. Its value
is that it spends cheap context on defects a fresh reviewer would otherwise spend its one cold pass
on.

## Method

The handoff asks the next reader to assume a further layer exists and to hunt it by asking what each
fix *depends on* rather than whether it is worded correctly. Three of the four findings below came
from that question. The fourth came from the instruction the handoff gives the closure reviewer —
write an evaluator from the published prose and run the property's own mutations through it, rather
than reading `C4-P2` and agreeing with it.

The evaluator encoded `C4-P2` from the contract's prose alone, with the settling-frame reference taken
two ways: the four fields the neutral brief states, and the three the interaction state machine and
the grid state. It was run over the two named mutations and the two cases the contract says must stay
green. Under the brief's four fields all four vectors behave as the contract claims — both mutations
red, both legal cases green — which is real evidence for the U1, W3 and Y4 line of corrections. Under
the machine's three fields the duplicate terminal is undecidable, which is AC1.

## Findings

### AC1 — Y4's fix landed only in the artifact that reads the fact — corrected

Y4 added the settling frame's arrival ordinal because kind, interaction identity, and committing
endpoint do not separate two frames of the same kind from one endpoint — which is exactly what a
duplicate terminal is, and a case `C4-P2` must leave green. The ordinal went into the neutral brief's
parity profile and local-observation schema, and into nothing else. The interaction state machine,
which owns the latch and where X1 put the recording rule, still named three fields; so did the grid's
late-traffic latch section, which the generated models enumerate; so did the owner row AB2 had just
added to the responsibility matrix.

That is not a stale restatement. The brief declares itself **subordinate** to the capability contract,
both state machines, and the grid — "if a convenient schema shape contradicts them, the schema
changes" — so the artifact hierarchy resolves this particular contradiction against Y4. Batch 2 reads
the machine, authors three fields, and the parity profile compares a fourth that no observation
carries: Y1's defect, restored by the fix for Y4.

**Corrected** in all three artifacts. The machine and the grid now record kind, interaction identity,
committing endpoint, and arrival ordinal, each stating why the first three are insufficient and
carrying Z1's restriction that the ordinal identifies and never orders. The matrix's crossing artifact
carries the ordinal too, so the owned fact and the compared fact are the same fact.

### AC2 — the detailed reason the first conjunct reads is not in the closed set it points at — corrected

V1 made the peer-fault detailed reason a normative comparison "wherever its category declares a closed
set of them", and named the `C4-P2` case as "one detailed reason of `invalid-interaction-correlation`
and not the category as a whole". The only artifact declaring that set is the migration ledger, and
its five values are missing, extra, wrong-session, reused, or mismatched identities. A control naming
an identity the recipient never accepted is none of them: the identity is not absent, not spurious,
not out of scope, not reused, and not unequal to another — it was never opened.

So the set is closed and the value the conjunct quantifies over is not in it. This is X1's
`state-violation` finding one category over — there the category declared no reason set at all, here
it declares one without the reason — and V1's own text quotes the five values without asking whether
they cover the case it was correcting for.

A second half surfaced with it. The conjunct quantifies over a **cancellation control**, and the
grid's `unseen` row gives the cancellation-control cell and the other-peer-event cell the same
provenance, while C10's enumeration for a frame that opens no interaction requires the refusal and its
provenance and not which kind of frame was refused. One reason value would not have separated them
either.

**Corrected** by declaring `unopened-interaction-identity` in the ledger's closed set with the reason
the five identity values do not reach it; by naming that reason in the parity profile rather than
describing it, since a value identified only by description is not comparable; by requiring C10's
observation of such a refusal to record the kind of frame refused; and by having both `unseen` grid
cells and the machine's `unseen` transition row assert the reason and the kind.

### AC3 — the property's subject was the wrong endpoint — corrected

Both conjuncts opened with "no endpoint **records**" and then said "the same endpoint had already
committed". The nearest antecedent is the recording endpoint, and the recording endpoint is never the
committing one: a recipient commits no requests, and an initiator commits no acknowledgement its own
latch settles against. Read literally, each conjunct quantified over an endpoint pair no vector can
produce, and a conjunct that cannot be satisfied cannot be violated — U1's defect arriving through a
pronoun.

The intended reading is recoverable from the sentence that follows and from the brief's precedence
relation, and the evaluator above used it. That is the whole objection: a reviewer instructed to write
an evaluator from the published prose has to guess once, and the two candidate readings differ by
whether the property can fail at all.

**Corrected** by making the committing endpoint the explicit subject of both conjuncts and adding the
gloss naming it, with the reason the other reading is not merely worse but vacuous.

### AC4 — the check written over the X7 class could not see two-letter families — corrected

X7 was closed with a check written deliberately over the general class rather than over the six W
ids: every finding a retained iteration review raises must appear in the completeness review's
disposition history. Its pattern matches one letter followed by digits. The AA and AB families already
existed when it was written, so it could not see them, and it could not have seen this review's own
findings either — a retained record whose enforcement silently skips it.

**Corrected** by widening the family pattern to one or two letters. This did not require retroactive
records for the AA and AB passes and none were written; see the residual note below.

## Checks

Each check was written and observed failing against the pre-correction artifacts before anything was
edited, and each failed with the message naming the mechanism claimed for it. Eleven failures were
observed across AC1-AC4 plus the retained-review roster.

Each was then mutation-tested by reverting the correction it pins and confirming it fires again.

| Check | Pins | Mutation reverted |
| --- | --- | --- |
| interaction machine latch section names an arrival ordinal | AC1 | remove the ordinal from the machine's latch passage |
| grid late-traffic latch section names an arrival ordinal | AC1 | remove the ordinal from the grid's latch passage |
| observation owner row carries the ordinal | AC1 | restore the matrix row's three-field crossing artifact |
| ledger declares `unopened-interaction-identity` | AC2 | restore the five-value reason set |
| parity profile names that reason | AC2 | restore "one detailed reason of `invalid-interaction-correlation`" |
| C10 requires the kind of frame refused | AC2 | restore "records the refusal and its provenance" |
| C4-P2 names the committing endpoint in each conjunct | AC3 | restore "the same endpoint had already committed" |
| C4-P2 carries the committing-endpoint gloss | AC3 | delete the gloss paragraph |
| disposition history covers two-letter families | AC4 | narrow the pattern to `[A-Z]` |

The three AC1 checks are scoped to the sections that have to carry the rule rather than to the
artifacts, because X1's own check was scoped that way after mutation testing found a phrase-anywhere
form satisfied by the artifact's status block. The AC3 pair is deliberately two checks — one requiring
the corrected subject, one rejecting the ambiguous phrasing — because a rewrite that dropped the
conjunct entirely would satisfy a requirement-only check.

**This evidence is worth less than it looks**, for the reason the W review already recorded: the
checks were written by the actor that wrote the corrections, from one reading of the same prose, and a
check can only ask a question someone thought to encode. Every blocking finding in this programme was
raised against artifacts whose gates were green. In particular the next reviewer should not accept
from this document that the arrival ordinal is now stated everywhere it is read, that
`unopened-interaction-identity` is the right granularity for the detailed reason rather than one value
where two were needed, or that the committing-endpoint gloss covers every quantifier in `C4-P2`.

## Residual, not corrected

**The AA and AB passes still have no retained iteration review.** The two-kinds-of-review section
requires an author-side pass to be retained as evidence — that requirement is X7 — and the sixth and
seventh passes left commit messages and step lists, which is the gap X7 named for the W passes. AC4
widened the check that should have caught it, but the check only fires on findings *inside* a retained
review, so it does not fire on a pass that retained none. Writing those records now would mean
authoring an account of passes this actor did not run, which is worse than the gap. It is left for the
owner: either the AA/AB reasoning is reconstructed by whoever ran it, or the requirement is scoped to
say that the disposition history is the retained record when a pass raises no separate findings.

**Two questions this pass did not settle**, both inherited and both now slightly larger. The
observation record is load-bearing for `C4-P2` and no artifact bounds its volume; AC2 has just added
two more required fields to each such record, which does not change the argument but does raise the
cost per refused frame. And the arrival ordinal is still an observation-local counter with no
statement about whether a frame refused before correlation consumes one — AC2's requirement that such
a refusal record its frame kind makes that record slightly more structured without answering it.

## Gates

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | passes |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | exactly one message, on `C12-P1` |
| `build/verify-doc-links.ps1` | passes |
| `build/verify-text.ps1` | passes |
| `build/verify-interchange.ps1` | exit 0; only the two pre-existing `Cbi51` restart-policy skips |

Gates passing is not a verdict, and an iteration review reporting no findings would not have been one
either. The work is ready to be reviewed.
