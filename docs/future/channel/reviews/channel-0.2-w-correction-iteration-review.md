# Channel 0.2 W correction iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-w-correction-iteration-2026-08-14-3dc50cf`

Reviewed work: the W1-W6 corrections, `fix(channel): close W1-W6, the layers under the U1 correction`,
and the U1/U2-U8/V1-V2 corrections beneath them; then, in further passes, the X1-X7 and Y1-Y4
corrections this document itself records

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

Eighteen findings across four passes — X1-X7, then Y1-Y4 against those corrections, then Z1-Z4 against
those, then AA1-AA3 against the entry points rather than the design package — all corrected. The review policy asks the next reviewer to assume a tenth layer exists under
the nine the U-V-W sequence found, and to hunt it by asking what each fix *depends on*. That method
produced every one of the fifteen; none would have been found by re-reading `C4-P2`, which is now
correct as a sentence and was correct as a sentence before any of these passes.

Two are the same shape as U1 itself — a fix whose own dependency was never checked — and they are the
two worth reading first. **X1**: W6 made the late-traffic latch comparable, and the conjunct that
motivated W6 does not read the latch. **X5**: W4 abolished the state the property's first conjunct
quantifies over, and nothing distinguished the state it meant to abolish from the record it needed to
keep.

Four passes is where this stopped, and the reason is worth stating plainly. The last two found things
no correction in this sequence introduced: **Z4**, a required evidence group missing from the ledger's
inventory, and **AA1**-**AA3**, five cycles of staleness in the two documents a reader meets before
any artifact. Both are holes the whole sequence walked past while auditing each other's fixes. That is
evidence the method still had reach when it was stopped, not evidence the artifacts are now clean. It
stopped because none of this is a verdict either way — that is the closure reviewer's to give, and an
author-side pass cannot substitute for it however many times it runs.

## First pass — X1-X7, the layer under the W corrections

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

## Second pass — Y1-Y4, the layer under the X corrections

The same method was then turned on the X corrections, and found four more. Three of them are one
question: **X1 and X5 made a property read facts, and nothing had been asked to carry them.**

### Y1 — the facts the parity profile now compares are in no observation — corrected

W6 added the late-traffic latch to the normative comparison and X1 added the frame that settled it.
C10 owns what an observation must be sufficient to distinguish, and its enumeration named neither. The
brief's local-observation schema — the Batch 2 artifact that would actually have the fields — named
neither either: provenance, state, admission decisions, dispatch boundary, terminal form, detection
point, effect certainty.

So the parity profile compared two fields that no observation was required to carry, which is V1's
defect moved down one floor: V1 was a fact the parity list did not compare, this is a fact the parity
list compares and the record does not hold. Two implementations would have compared absence with
absence and passed.

**Corrected** in C10 and in the local-observation schema, with the latch position holding the explicit
`not-applicable` X2 introduced rather than going absent.

### Y2 — C10 does not reach the refusal X5 depends on — corrected

C10 requires an observation for "every attempted establishment and interaction". The `unseen` refusal
is neither: no interaction exists there, which is the whole of W4 and the reason X3 needed its own
transition row. X5 asserted the observation in C4, in the provenance table, and in the grid — and left
the capability that owns observation content silent about it, so the one record `C4-P2`'s first
conjunct reads was mandated by the capability that reads it and by nothing that owns it. That is the
S1 shape exactly, and S1 is the finding this entire sequence descends from.

**Corrected** by giving C10 the case: a recognized frame that opens no interaction yields an
observation too.

### Y3 — the state X3 routes to is terminal, and every terminal state owns a latch — corrected

X3 added the missing recipient transition row and sent it to `rejected-protocol`. The recipient state
table marks `rejected-protocol` terminal; the two `any terminal` rows claim every terminal state; and
what they do is apply the late-traffic latch. So the row that existed to keep the catch-all from
manufacturing a latched terminal interaction manufactured one itself, at the destination instead of
the route.

The resolution is forced rather than chosen: W4 already says nothing is retained and that a later
request bearing the identity arrives at `unseen` like any other first request. If nothing is retained
there is no state to sit in, so the recipient's per-identity state remains `unseen` and
`rejected-protocol` is the provenance the refusal is recorded under. That also turns W4's
later-request sentence from an assertion into a consequence.

**Corrected** in the transition row and in the late-terminal section.

### Y4 — the settling-frame reference is ambiguous exactly where the property must stay green — corrected

X1's reference named the settling frame by kind, interaction identity, and committing endpoint. One
endpoint may commit two frames of the same kind for one identity, and that is not a corner case: it is
the duplicate terminal from a nonconformant peer, which `C4-P2` must leave green and which the
contract names as the reason each conjunct is restricted to one endpoint's own frames. Bound to the
earlier of the two matching steps, the property reads "committed before the terminal frame" and goes
red on legal input.

This one was recorded as an open question at the end of the first pass rather than as a finding. It
became a finding when the ambiguous case was named concretely, which is the difference the review
policy draws between a hypothetical and a test case.

**Corrected** by adding the settling frame's arrival ordinal within the interaction, which maps it to
exactly one declared step — directly where no reordering is injected, and through the named injection
where one is.

## Third pass — Z1-Z4, the layer under the Y corrections

### Z1 — the ordinal Y4 added is the observed order W1 excluded — corrected

W1 made the precedence relation deliberately narrow: declared steps only, never an observed time,
arrival order, or cross-endpoint relation, because Channel promises no order across endpoints and owns
no clock. Y4 then made an arrival ordinal a compared normative field. It is there to identify which
received frame settled a latch, and nothing said so, so the property language acquired an
observed-arrival operand of exactly the kind W1 had removed — under cover of a field added for another
purpose. A property could have ordered two endpoints' frames by ordinal and asserted an ordering the
contract does not have.

**Corrected** by restricting the ordinal to identification: compared for equality, never an operand of
precedence or any other comparison that reads it as an order.

### Z2 — the grid cell Y3 contradicted still reads as a next state — corrected

Y3 settled that the refusal leaves the recipient's per-identity state at `unseen` and records
`rejected-protocol` as provenance. The grid's `unseen` row still named `rejected-protocol` in the cell
format every other row uses for a next state. One token, two meanings, two artifacts — which is S1's
shape, in the artifact S1 was raised against.

**Corrected** by naming the provenance as a provenance in both `unseen` cells, and by saying why the
same token at `validating` does name a state: there an interaction exists to be in it.

### Z3 — Y1 left the one latch value it introduced unowned — corrected

X2 introduced `not-applicable` and the parity profile compares it. Y1 gave C10 the latch and the
settling frame and stopped at "the terminal interaction's" latch, so the value a non-terminal route
asserts was compared and owned by nothing — Y1's own defect surviving in the corner Y1 did not sweep.

**Corrected** in C10, which now carries the value and the reason an absent field would conflate a
route with no latch and a latch that has not settled.

### Z4 — the evidence this whole sequence exists to produce is in no inventory — corrected

The migration ledger's new-evidence section lists the 0.2 cases with no 0.1 predecessor, and that is
where Batch 2 learns what must be built beyond the migrated set. Intra-interaction frame order was not
in it. Channel 0.1 promised no order, so there is no predecessor vector to carry the requirement in by
another route, and the vector group U3 added to the brief is a different inventory.

So the requirement every finding since S1 turns on was absent from the list of what Batch 2 must
build. This one was not introduced by any correction in the U-Y sequence; it is a hole those
corrections went past, and it was found by asking where else the mutations have to be written down.

**Corrected** by listing the group, both mutations, the green cases, and the new observation fields
that have no 0.1 field to migrate from.

## Fourth pass — AA1-AA3, the entry points nobody re-read

The first three passes audited each other's corrections and never left the design package. This pass
asked a different question — what a reader who opens none of these artifacts is told — and found the
two indexes had fallen behind every one of the five correction families.

### AA1 — the Channel index stopped at V2 — corrected

The index's summary named B1 through V1-V2 as the corrected set, called the pending cycle "the S1
correction", said seven reviews were retained when the directory held eight attestations and two
iteration reviews, and carried per-artifact state rows that stopped at S2. Every status check in the
design verifier passed across all of it, because those checks ask whether the required phrase is
present and whether a superseded cycle name is absent — and the index said "fresh independent closure
re-review" throughout while being wrong about everything else.

**Corrected**, and the two claims that can be checked structurally now are: the review counts are
computed from the directory and compared as digits, and every finding family the completeness
review's disposition history records must appear in the index. Prose staleness beyond that is still
only caught by reading.

### AA2 — the future-work index stopped one cycle earlier — corrected

The same defect one document over, and worse: the future-work index is what a reader consults while
choosing what to work on, and it said R1 was closed at `validating` and not at `unseen`, "which is
blocking finding S1" — four correction families after S1 closed. Its review count was written as a
word, which is how it went stale unnoticed.

**Corrected**, with the same two structural checks applied to it.

### AA3 — `channel-core` survived U2 in the index — corrected

U2 closed the responsibility matrix's owner vocabulary because the S1 correction had introduced
`channel-core` as a second identifier for the family every other row calls `channel`. The future-work
index still attributed the ordering row to `channel-core`. A closed vocabulary that is closed in one
artifact is not closed: a Batch 2 ownership inventory keyed by identifier would still find two names
for one owner in the repository, which is the exact failure U2 was raised to prevent.

**Corrected** in the index, and the identifier is now rejected in every Channel status entry point
rather than only inside the matrix.

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

The sharpest unanswered question these passes leave: **the observation record is now load-bearing for
a property, and no artifact bounds it.** X5 argues it is not the state R1 refused because nothing
consults it, and Y1/Y2 then made C10 require the record and gave it fields. That is a statement about
what reads the record, not about how much of it there is, and a peer naming unopened identities still
causes one observation per name. Whether the design owes an explicit bound on observation volume — or
an explicit statement that observation volume is a host concern outside Channel — is an owner call
these passes did not make, and it is now larger than it was, because two more capabilities depend on
the record existing.

Two smaller ones. The arrival ordinal added under Y4 is an observation-local counter, and no artifact
says whether a frame refused before correlation consumes one. And `not-applicable` is now a compared
latch value on exactly one route; whether a second such route would be recognised as needing it, or
would quietly go absent, rests on the grid's prose rather than on anything structural.

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
