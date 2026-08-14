# Channel 0.2 W correction iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-w-correction-iteration-2026-08-14-3dc50cf`

Reviewed work: the W1-W6 corrections, `fix(channel): close W1-W6, the layers under the U1 correction`,
and the U1/U2-U8/V1-V2 corrections beneath them

Date: 2026-08-14

**This is an iteration review, not an attestation.** Under
[Two kinds of review](./README.md#two-kinds-of-review) it is author-side work: it shares a context
with the corrections it examines and it corrected what it found in the same pass. It **does not close
the first batch, does not authorize Batch 2, does not produce the closure record, and its verdict is
not the conforming verdict the Closure section requires**.

Status of the work after this pass: **ready to be reviewed, not reviewed.**

## Why this record exists at all

The W1-W6 passes left no retained iteration review. Their record was a commit message and a step list
in the review policy, and the disposition history that carries V1 and V2 stopped before them. That is
itself a finding — **X7** below — and it is corrected here rather than repeated: this document is the
retained record for the W pass's dispositions as well as for its own.

## Verdict

Seven findings, all corrected. The review policy asks the next reviewer to assume a tenth layer exists
under the nine the U-V-W sequence found, and to hunt it by asking what each fix *depends on*. That
method produced all seven; none would have been found by re-reading `C4-P2`, which is now correct as a
sentence and was correct as a sentence before this pass.

Two of the seven are the same shape as U1 itself — a fix whose own dependency was never checked — and
they are the two worth reading first. **X1**: W6 made the late-traffic latch comparable, and the
conjunct that motivated W6 does not read the latch. **X5**: W4 abolished the state the property's
first conjunct quantifies over, and nothing distinguished the state it meant to abolish from the
record it needed to keep.

## Findings

### X1 — the parity profile compares the latch value; the conjunct reads the frame the latch settled against — corrected

`C4-P2`'s second conjunct forbids "a late-traffic `state-violation` **latched against a frame** the
same endpoint committed before the frame that made the interaction terminal". W6 observed that the
latch was demanded as grid evidence and never compared, and added the latch *value* to the normative
parity list. The latch value is `clear`, `fault-committed`, or `fault-unavailable`. It names no frame.

Three runs record the identical comparable observation — terminal preserved, peer-fault category
`state-violation`, latch `fault-committed`:

1. `C4-outcome-precedes-ack`, the mutation the conjunct must go red on;
2. a legal late control arriving after a peer's terminal, which the contract says must leave the
   property green; and
3. a duplicate terminal from a nonconformant peer, likewise.

V1's detailed-reason clause does not reach them either: it is conditional on the category declaring a
closed set of detailed reasons, and the migration ledger declares those for
`invalid-interaction-correlation` and none at all for `state-violation`. So the conjunct could not
separate the case it must fail on from the two the same paragraph promises it will not — which is U1's
defect with the sign flipped, a property that goes red on legal input or on nothing, depending on how
an evaluator resolves an antecedent it cannot bind.

**Corrected** by recording the settling frame — kind, interaction identity, and committing endpoint —
where the latch settles, in the interaction machine and the grid that own the latch, and comparing it
in the brief's normative parity list. Once the settling frame names its committing endpoint, the
precedence relation W1 added can bind it to a declared stimulus step, which is what the conjunct
needs and the latch value never was.

### X2 — W4 created a cell with no latch; the grid requires every cell to assert one — corrected

The grid's evidence requirement is that "each cell asserts next state, ... and late-traffic latch",
and W6's justification for the parity addition quotes it. W4, in the same commit, gave one route no
latch at all. A required assertion with no value is the silence Decision 10 names: one implementation
writes `clear`, the other writes nothing, both defensibly, and every cross-stack comparison passes.

**Corrected** by making the absence an explicit `not-applicable` value that the cell asserts and the
parity profile compares, rather than an absent field.

### X3 — the machine that is the detailed authority has no row for the event the property is about — corrected

The grid says the state-machine transition tables remain the detailed authority, and its own totality
rule 1 is that a matching detailed transition row wins. The recipient transition table had exactly one
row from `unseen`, for a request. A cancellation control at `unseen` therefore had no detailed row,
and the interaction machine's own totality rule routes a recognized peer event in a nonterminal state
to an interaction-scoped `state-violation` and a terminal `peer-fault`.

That is a terminal interaction; a terminal interaction owns a latch; and the state is `peer-fault`,
not `rejected-protocol`. Three contradictions of W4 and of the grid cell, about the exact event
`C4-P2`'s first conjunct quantifies over — and W4 corrected the prose of the machine's late-terminal
section without noticing that the machine's tables never routed the event there. The fact the property
reads was not derivable from the artifact that owns it.

**Corrected** by adding the recipient transition row, and by saying in both artifacts why it must be a
detailed row rather than a catch-all: the catch-all produces precisely the terminal interaction W4
refuses.

### X4 — the second conjunct's mutation is in no required vector group — corrected

W3 added `C4-outcome-precedes-ack` because half a property with no named mutation is half
unfalsifiable. U3 added the required adversarial vector group that makes the *first* mutation exist in
Batch 2. Nothing added the second to it, and the group's own text still claimed
`C4-control-precedes-request` was "the only vector group whose expected observation is a property
going red".

A named mutation absent from the required groups is a mutation no suite has to contain, so W3's fix
stopped at the contract. This is U3 one layer down, in the same way V2 was.

**Corrected** by requiring both mutations in that group, one per conjunct, along with the two
latch-settling cases the property must leave green.

### X5 — the witness is a record W4 abolished — corrected

W4 says the recipient, refusing a control at `unseen`, "commits one interaction-scoped peer fault and
keeps nothing". `C4-P2`'s first conjunct quantifies over what an endpoint **records** there. Either
the witness does not exist or "keeps nothing" is false, and no artifact said which.

The reconciliation is real but was stated nowhere: an observation is written once as evidence and
never consulted, while the state the R1 ruling refused is state a later decision would have to read —
which is exactly what made it accruable by a peer naming identities it never opens. Both halves depend
on that distinction. Without it, either W4 abolishes the property's only witness, or the observation
record reintroduces the unbounded per-identity state R1 refused by another name.

The machine's terminal-provenance table made the gap concrete: it fixes provenance for terminal
histories, and W4 says this refusal is not one, so the one record the property reads had no declared
provenance anywhere.

**Corrected** in C4, in the provenance table, and in the grid cell: one local observation is recorded,
recording is not retaining, nothing consults it, and it is the record `C4-P2` reads.

### X6 — the pin clause went stale one commit after being rewritten to close U6 — corrected

U6 was a review-target clause naming a commit later work had superseded. The rewritten clause names
`fix(channel): close U2, U3, U4, U7, and U8` as the current review target, and the W1-W6 commit then
changed six design artifacts — contract, interaction machine, grid, brief, completeness review, and
policy. The clause's own escape hatch, "or any later commit whose design artifacts hash identically to
it", does not apply, and its own instruction to "check that claim rather than assuming it" was not
applied to itself.

The clause is structurally prone to this: it is written in the commit before the one that supersedes
it. So the correction is not only a new subject line.

**Corrected** by repointing the clause and by adding a check that compares it against the repository
rather than against its own wording: the named target must be the most recent commit that changed a
design artifact. The check skips only while those artifacts have uncommitted edits, because a pin
cannot name a commit that does not exist yet.

### X7 — two iteration passes left no retained evidence — corrected

The two-kinds-of-review section this branch added requires an iteration review to be "retained as
evidence, named `*-iteration-review.md`". The W1-W6 passes produced none. Their record is a commit
message and a step list, and the completeness review's disposition history — which carries V1 and V2 —
stopped before them. The retained-file check enumerates review files by exact name, so an absent record
is invisible to it by construction.

Recording the V3 disposition had also been missed: V3 was raised, deliberately left uncorrected as an
owner call, and never reached the disposition history where the next reviewer would look for it.

**Corrected** by this document, by recording the W and X dispositions in the completeness review, and
by a check written over the class rather than the instance: every finding a retained iteration review
raises must appear in the disposition history, and a finding family the policy attributes to an
iteration pass must have a retained record.

## Disposition of W1-W6, recorded here because their pass did not record it

| Finding | Disposition |
| --- | --- |
| W1 | Corrected: the closed property operator set gained a bounded precedence relation over one endpoint's declared stimulus steps. Re-examined here and still sound; X1 is about a fact the relation had nothing to bind to, not about the relation. |
| W2 | Corrected: the reordering provider declares per-interaction frame order and then violates it, and establishment verifies presence rather than truth. Re-examined here; unchanged. |
| W3 | Corrected in the contract; **incomplete in the vector suite** until X4. |
| W4 | Corrected in the contract, machine, and grid; **contradicted by the machine's own tables** until X3, and **at odds with the property's witness** until X5. |
| W5 | Corrected: stimulus steps name their committing endpoint. Re-examined here; the settling frame added under X1 names its committing endpoint for the same reason. |
| W6 | Corrected as to the latch value; **the conjunct reads the settling frame**, which is X1. |

## What was re-verified, and what that is worth

Each of the seven checks was written before its correction, observed failing against the pre-correction
artifacts, and mutation-tested afterwards by reverting the correction and confirming the check fires
again. One check found more than it was written for: the disposition-history check, written over the
general class for X7, fired on V3 as well, which no one had noticed was unrecorded.

**That evidence is worth less than it looks.** The checks were written by the same actor that wrote the
corrections, from the same reading of the same prose. A check can only ask a question someone thought
to encode, and every prior cycle's blocking finding was raised against artifacts whose gates were
green. In particular the next reviewer should not accept from this document that the settling frame is
sufficient to make the second conjunct evaluable, that `not-applicable` is the right disposition for a
latch that does not exist, that the `unseen` transition row is complete for every recognized peer event
that can arrive there, or that recording-is-not-retaining bounds the observation record as claimed —
all four are this author's conclusions about this author's corrections.

The sharpest unanswered question this pass leaves: **the observation record is now load-bearing for a
property, and no artifact bounds it.** X5 argues it is not the state R1 refused because nothing
consults it. That is a statement about what reads the record, not about how much of it there is, and a
peer naming unopened identities still causes one observation per name. Whether the design owes an
explicit bound on observation volume — or an explicit statement that observation volume is a host
concern outside Channel — is an owner call this pass did not make.

## Gates

Run after the corrections:

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | passes |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | exactly one message, on `C12-P1` |
| `build/verify-doc-links.ps1` | passes |
| `build/verify-text.ps1` | passes |
| `build/verify-interchange.ps1` | exit 0; only the two pre-existing `Cbi51` restart-policy skips |

Gates passing is not a verdict, and an iteration review reporting no findings would not have been one
either. The work is ready to be reviewed.
