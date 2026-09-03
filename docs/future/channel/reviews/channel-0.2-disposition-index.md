# Channel 0.2 disposition index

Date: 2026-08-18

Status: retained review record. **Not a design artifact**, not part of the reviewed package, and
assessed by no closure review. It owns the per-artifact correction history that each design
artifact's status block used to carry.

This file exists because of **W3** of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md). Every
correction added sentences to the status block of the artifact it touched, so the surface a cold
reviewer had to read grew each time a defect was repaired -- the plan's section 1.3. Between them the
nine status blocks had reached the line count
[the plan's section 4 measures](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#4-what-to-measure),
and none of it states what the artifact *says*; all of it
states what was once wrong with it and is no longer. That number is cited rather than restated: this
file said **289** for as long as the plan did, **AM2** corrected the plan's two statements of it to
**265**, and this third one stood -- which is **AN5**, and is the shape the plan's own W1 exists to
retire. That history is worth keeping and is not worth
re-reading on every cycle, so it lives here and each artifact's status block points at it.

**Nothing here was rewritten.** Each section below is the text that stood in that artifact's status
block at commit `9ce01a0`, moved verbatim. Where a claim was true there it is true here, and where
one was awkward it is still awkward: a move that paraphrased would be a fresh statement of a fact the
retained attestations already own, which is the failure this file is part of retiring.

The authority for what a finding *was* remains the record that raised it -- the
[retained attestations and the review policy](./README.md). This index says which artifact carries
which disposition; it does not restate a finding, and where it disagrees with an attestation the
attestation is right.

## How to read a status block now

An artifact's status block states what the artifact is and what it awaits, and links here. It does
not carry disposition history. `build/verify-channel-0.2-design.ps1` enforces both halves: a status
block over five lines fails, one that does not resolve to a section here fails, and a section here
that does not reach the newest disposition family fails. That single check replaces **AI4** and
**AH3**, which policed the same freshness across nine blocks and the plan's separately.


## Redesign and migration plan

[Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md](../Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md)

**Status:** First-batch design foundation drafted and its four owner rulings resolved. B1-B4, N1-N3,
F1-F3, D1-D5, T1-T4, and R1-R3 are closed as framed, the last three re-verified by the seventh review
in the artifacts they were raised against. That review's blocking finding S1 — that the R1 correction
kept `rejected-protocol` at recipient `unseen` under a delivery-ordering guarantee stated in the
state/event grid alone, which C4 and C11 disclaimed and the responsibility matrix assigned to
`delivery-facet` — is corrected under the 2026-08-13 S1 ruling: Channel 0.2 core owns intra-interaction
frame order, narrowly scoped, stated in C4 with `C4-P2` and a mutation vector, given an owner row in
the responsibility matrix, and declared by the realization profile. Nonblocking S2 and S3 are
dispositioned in the same pass. The eighth review then found S1 closed as to ownership and not as to
falsifiability and raised blocking **U1** with nonblocking **U2**-**U8**; those are corrected, as are
**V1**-**V3**, **W1**-**W6**, **X1**-**X7**, **Y1**-**Y4**, **Z1**-**Z4**, **AA1**-**AA3**,
**AB1**-**AB2**, and **AC1**-**AC4**, every one raised by an author-side iteration pass over the
previous corrections and none by an independent review. AB1 is this status block, which had stopped at
S3 while six passes ran. AC1-AC4 are the layer under the Y and V corrections — the arrival ordinal
stated only in the artifact that reads it, a closed detailed-reason set with no value for the refusal
`C4-P2` quantifies over, the property's own subject naming the wrong endpoint, and a class check blind
to two-letter finding families. **AD1**-**AD3** then turned the same method on the retained records
themselves: AD1 is the AC pass's residual denying that the AA and AB evidence existed and referring
the gap to the owner, AD3 the three disagreeing accounts of what the W iteration review contains, and
AD2 the half of the X7 class check still written over two ids. The ninth closure review then returned
`does-not-conform` with blocking **AE1** and nonblocking **AE2**-**AE5**, and ruled AD2 a defect; all
six are corrected, AE1 under the dated ruling recorded below. The tenth closure review then returned
`does-not-conform` with blocking **AF1** and nonblocking **AF2**-**AF8**: it confirmed the AE1
property fix works and found the correction incomplete one artifact below itself, in the passage
stating what the mutation vectors' expected observations are. The eleventh raised blocking **AG1**
with nonblocking **AG2**-**AG5**, and the twelfth returned `conforms-with-nonblocking-findings` with
**AH1**-**AH6** and no blocking finding — the first non-negative verdict in the programme. All are
corrected.
A fresh independent closure re-review of that whole sequence precedes Batch 2. No Channel 0.2
implementation or ratification is claimed.
The thirteenth review then returned `does-not-conform` with blocking **AI1** and nonblocking
**AI2**-**AI9**: the settling-frame reference — one of `C4-P2`'s two operands — was still published as
four fields with no session after AH1 had made two-session vectors legal. Under **AI5** and **AI9**
this plan is corrected there: the AH1 ruling's citation of reconnect cases C2 does not have, and
section 7.8's report of seven retained negative attestations, which was S3's own evidence and had
stayed open for six cycles while every index called the programme's findings closed. This block also
claimed **AI2** was corrected here; it was not, because AI2's two narrative surfaces are the
future-work index and the Channel index, and both were unchanged. That claim is withdrawn under
**AJ2**, which is the fourteenth review's finding that a disposition was recorded in an artifact the
finding was never raised against while remaining open in both artifacts it was.
The fourteenth review returned `does-not-conform` with blocking **AJ1** and nonblocking
**AJ2**-**AJ7**. **AJ1** is AI1 surviving its own correction commit: the reference is published in
five places and the correction reached three, leaving the state/event grid the neutral brief is
subordinate to and the responsibility matrix row that owns the observation record, from both of which
the reviewer reproduced AI1's exact false green. All seven are corrected.
The fifteenth review returned `does-not-conform` with blocking **AK1** and nonblocking
**AK2**-**AK4**, and confirmed AJ1 closed by evaluator. **AK1** is the same shape as AI1 and AJ1 on
`C4-P2`'s **other** conjunct: the recorded `unseen` refusal is that conjunct's operand, five surfaces
published what it contains, and none named the session AF8's membership scope requires or the identity
the test is over, so the property was red on a two-session vector conforming at both endpoints. All
four are corrected. The correction pass then enumerated `C4-P1` and `C4-P2` completely instead of
sampling one operand as the four previous cycles had, and that enumeration — retained in the
completeness review — raised **AK5** and **AK6**, the rest of the first conjunct's operand and the
second conjunct's second precedence operand, which no artifact published at all; and **AK7** and
**AK8**, which are AH1's multi-session decision never having reached the property statements, so
`C4-P1`, `C4-P2`, `C1-P1`, `C3-P1` and `I5` each counted a per-session fact across the vector.
The sixteenth review returned `does-not-conform` with blocking **AL1** and **AL2** and nonblocking
**AL3** and **AL4**, and confirmed the AK corrections by evaluator where they are measurable: its
`C4-P2` evaluator is correct on every input it ran, and its row-by-row audit of the operand
enumeration found no row missing and none wrong. **AL1** is AK7's own defect on a sixth property —
`S3` bounded admission by "the first drain transition" and named no session, so a second session
legally establishing and admitting after the first drains takes it red — and it survived because the
AK audit recognises a per-session fact by that fact's own words while `S3` reads one through a
transition of it, the session's own state being absent from C12's declared list, which is **AL3**.
**AL2** is the refused-frame reference published in five surfaces and corrected in four, the missed
surface being the state/event grid's two `unseen` cells, which neither half of the AK1 check could
read because both key on the reference's name. **AL4** is `S5` comparing a per-session profile across
the vector. All four are corrected, and the two checks written for them are structural rather than
lexical: every property of the session state machine must name its session because that machine's
properties are statements about one session by construction, and the sweep for the refusal record is
keyed to the record rather than to the reference's name.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

Active plan; AB1 and AG3 corrected, the latter recording the AE1 ruling as issued and its AF8 narrowing; AH3 corrected and the 2026-08-15 closure-standard ruling recorded; AI5 and AI9 corrected, the last a retained S3 evidence surface open for six cycles; AJ2 corrected — this row and the plan's status block both claimed AI2, whose two surfaces are the narratives in this index and the future-work index and were unchanged, and that claim is withdrawn; AK1's dated AE1 ruling records that AF8's session qualifier had no operand until this pass, and the status block carries the AK1-AK8 sequence; under **AL1**-**AL4** the status block and its narrative carry the sixteenth review and the AL family; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## C1-C12 capability contract

[Brontide-Channel-0.2-Capability-Contract-0.1.md](../Brontide-Channel-0.2-Capability-Contract-0.1.md)

Status: proposed first-batch behavioral contract; N2, F1/F2, D1-D4, T3, R1, S1, S2, U1, W2, W3, W4,
X1, X5, Y1, Y2, Z3, AC2, and AC3 corrected after independent review. Under AC3 both `C4-P2` conjuncts
name the committing endpoint as their subject, because "the same endpoint" resolved to the endpoint
that records a refusal and never commits the frame in question, which made the conjuncts unsatisfiable
and therefore unfalsifiable. Under AC2 C10 requires the observation of a frame that opens no
interaction to record the kind of frame refused and the detailed reason
`unopened-interaction-identity`. Under Y1 and Y2, C10 requires an observation to
distinguish the late-traffic latch and the frame that settled it, and requires one for a recognized
frame that opens no interaction — the two facts `C4-P2` reads and the capability that owns observation
did not carry. C4 now owns intra-interaction frame order with
`C4-P2`, and C4's silence and C11 are scoped to cross-interaction and cross-session ordering. Under
the U1 correction `C4-P2` is stated over the refusal a reordering produces rather than over the
accepted sequence, because the design refuses a reordered frame and the accepted sequence can
therefore never be out of order. It carries one named mutation per conjunct under W3, and under W4 an
identity refused at `unseen` retains no interaction history and no latch. Under X5 that refusal still
records one local observation, which is the witness the property reads and is evidence rather than
retained state; under X1 the second conjunct's witness is the frame a late-traffic latch settled
against rather than the latch value. No Channel 0.2 schema, API,
implementation, or ratification is authorized until the complete design foundation receives a fresh
independent closure re-review. Under **AI6** the membership sentence names its own subject rather than
relying on the nearest antecedent, which is AC3's class in the paragraph AC3 was raised against. C10
delegates the settling-frame field list to the artifacts that publish it and states the fact alone, so
this contract is unchanged by the **AJ1**-**AJ7** corrections. Under **AK1** and **AK5** the record
`C4-P2`'s first conjunct quantifies over carries the refused-frame reference, so AF8's session
qualifier, the identity the membership test is over, and the committing endpoint the precedence half
reads all have operands; under **AK6** the observation names the frame an interaction's terminal
history was accepted on, which is the second conjunct's other precedence operand and was published by
nothing; and under **AK7** and **AK8** `C4-P1`, `C4-P2`, `C1-P1`, `C3-P1` and `C11-P1` name the session
they mean, because AH1 made a vector able to carry more than one and C12 now declares which facts
belong to one session each. Under **AL3** that declaration carries a fifth fact — the session's own
state — and is checked against the neutral brief's vector format rather than against itself: its four
members were the four facts read by the properties the AK pass had found red, and `S3` read a fifth
through a transition of it and was reachable by no pattern built from those four. **AL1**, **AL2** and
**AL4** are corrected in the session state machine and the state/event grid.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

N2/F1/F2/D1-D4/T3/R1/S1/S2/U1/W2-W4/X1/X5/Y1/Y2/Z3 corrected; C4 owns intra-interaction frame order with `C4-P2`, stated over the refusal a reordering produces; AE1/AE3/AF1/AF5/AF8 corrected, and AG2's cross-artifact claim is now pinned against the brief; AH6 corrected; AI6 corrected; unchanged by AJ, since C10 delegates the settling-frame field list to the artifacts that publish it; AK1/AK5/AK6 corrected - C10 requires the refused-frame and terminal-frame references and delegates their field lists the same way, and AK7/AK8 give `C4-P1`, `C4-P2`, `C1-P1`, `C3-P1` and `C11-P1` the session they mean while C12 declares which facts belong to one session each; **AL3** corrected - that declaration now carries the session's own state and is checked against the neutral brief's vector format rather than against the members that were visible when it was written; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Session state machine

[Brontide-Channel-0.2-Session-State-Machine-0.1.md](../Brontide-Channel-0.2-Session-State-Machine-0.1.md)

Status: proposed first-batch design artifact; D1 corrected after independent review and subject to a
fresh independent closure re-review. Unchanged by the **AI1**-**AI9** and **AJ1**-**AJ7** families;
the claim is stated over each family rather than over its last finding, because "unchanged by AI9" is
a true statement about one finding and a false impression about the family, which is **AJ5**.
Corrected for **AL1** and **AL4**: all six of `S1`-`S6` now name the session they are about, and `S5`
names the one declared profile its two establishment paths are compared over.

This status block previously recorded that the AK pass had audited `S1`-`S6` against C12's newly
declared per-session facts and that none of the six named one, "because the session machine's
properties are about one session by construction". The first half was true and the second is the
defect: the same argument was available for `I5` — an interaction belongs to one session by
construction too — and **AK7** rejected it and required `I5` to name the session all the same. `S3`
read one session's own drain transition across the vector and was red on a conforming two-session
vector while this block reported the audit clean. That is **AL1**, and the audit that missed it could
not have found it, because its trigger set is C12's declared fact list and the session's own state was
absent from it (**AL3**).

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

D1 corrected; unchanged by the sixth, seventh, and eighth reviews and by the U-Z correction passes; unchanged by the AE, AF, and AG passes, though AF7 brought S1-S6 into the property audit; unchanged by AH; unchanged by AI; unchanged by AJ; **AL1** and **AL4** corrected - all six of S1-S6 name the session they are about, and S5 names the one declared profile its two establishment paths are compared over; the status block previously recorded the AK audit as having found none of S1-S6 reading a per-session fact, which was true of the four facts that audit could recognise and false of the artifact, since S3 read one session's drain transition across the vector; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Interaction state machine

[Brontide-Channel-0.2-Interaction-State-Machine-0.1.md](../Brontide-Channel-0.2-Interaction-State-Machine-0.1.md)

Status: proposed first-batch design artifact; B1/B2, N2, F1/F2, D2/D3/D4, T3, R1, R2, S2, W4, X1, X3,
X5, Y3, AC1, and AC2 corrected after independent review and subject to a fresh independent closure
re-review. Under AC1 the settling frame this machine records carries its arrival ordinal, which Y4 had
added to the neutral brief alone while the brief is subordinate to this artifact; under AC2 the
`unseen` refusal records its detailed reason and the kind of frame refused.
Under Y3 the refusal leaves the recipient's per-identity state at `unseen` and records
`rejected-protocol` as provenance, because routing it to that terminal state would hand it back to the
`any terminal` rows and their latch.
`validating` now carries loss and drain rows, the pre-dispatch loss rule is reconciled to any
nonterminal state, and under W4 an identity refused at `unseen` is not a terminal interaction and owns
no latch. Under X3 that event is a recipient transition row of its own, because the totality rule
would otherwise make it the terminal interaction W4 refuses; under X5 the refusal records one local
observation whose provenance this artifact fixes; and under X1 settling the late-traffic latch records
the frame that settled it, which is what `C4-P2` reads and the latch value is not. Under **AI1** the
settling frame this machine records carries its **session**: AH1 declared a vector may carry more than
one, an interaction identity is unique only within one, and without the session two steps in different
sessions match every other published field. Under **AJ1** this artifact publishes that reference in
the one form every other publishing artifact uses, and under **AJ6** the paragraph beneath the field
list names the fields its argument is about instead of counting them from the front — the AI1
insertion had made "the first three" a set that omits the committing endpoint the claim is over.
Under **AK1** and **AK5** the `unseen` refusal records the refused-frame reference rather than a
reason and a frame kind, so all three of `C4-P2`'s first-conjunct qualifiers have operands; under
**AK6** accepting a terminal history records the terminal-frame reference, which is the second
conjunct's other precedence operand and was published by no artifact; and under **AK7** `I5` names the
session whose bound it is about. Unchanged by **AL1**-**AL4**: the sixteenth review's two blocking
findings are the session machine's property statements and the state/event grid's two `unseen` cells,
and this machine's own `unseen` transition row was already among the surfaces publishing the
refused-frame reference in full. Its `I1`-`I7` were re-read against AL1's question and each names the
interaction identity, which C12 declares per-session, so the AK7 recognizer reaches them.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

B1/B2/N2/F1/F2/D2-D4/T3/R1/R2/S2/W4/X1/X3/X5/Y3 corrected; `validating` carries loss and drain rows, and an identity refused at `unseen` retains nothing and owns no latch; AE2 corrected; unchanged by AF and AG; unchanged by AH; AI1 corrected - the settling frame carries its session; AJ1 and AJ6 corrected - the reference is published in the one form all six publishing artifacts use, and the paragraph beneath it names its fields instead of counting them; AK1/AK5 corrected - the `unseen` transition row records the refused-frame reference rather than a reason and a frame kind; AK6 corrected - accepting a terminal history records the terminal-frame reference, the second conjunct's other precedence operand; AK7 corrected in `I5`; unchanged by AL, whose two blocking findings are in the session machine's properties and the state/event grid's `unseen` cells, both of which this machine's own `unseen` row already published in the corrected form; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## State/event coverage grid

[Brontide-Channel-0.2-State-Event-Coverage-0.1.md](../Brontide-Channel-0.2-State-Event-Coverage-0.1.md)

Status: proposed first-batch totality artifact; added after D1-D4, corrected for T3, R1, R3, S1, S2,
U8, W4, X1, X2, X5, Z2, AC1, and AC2, and subject to a fresh independent closure re-review. Under AC1
the latch section records the settling frame's arrival ordinal, which Y4 had stated in the neutral
brief alone; under AC2 both `unseen` cells assert the detailed reason
`unopened-interaction-identity` and the kind of frame refused, which one shared provenance could not
distinguish. Under W4 the
`unseen` cancellation refusal retains no history and no latch, so the `any terminal` row does not
reach it; under X2 its cell asserts the latch as an explicit `not-applicable` rather than leaving a
required field absent, under X5 it asserts the one local observation it does record, and under Z2 its
cells name `rejected-protocol` as the provenance it is rather than as the next state every other row's
cells name. The intra-interaction ordering fact the
`unseen` verdict depends on is carried here and owned by C4. Under U8 the pre-dispatch Local loss cell
names `lost` like every other cell in that column, rather than leaving the state to be read out of the
interaction machine's totality rule. Under **AJ1** the latch section's settling-frame reference carries
its **session**, and under **AJ6** the sentence justifying the arrival ordinal names the fields it is
about instead of counting them. This status block previously declared the artifact unchanged by every
pass through AI9, which was false when it was written: AI1 changed the reference this grid publishes
and the AI1 correction reached three of the five artifacts that publish it, leaving the two the design's
own hierarchy resolves in favour of — this grid, which the neutral brief declares itself subordinate
to, and the responsibility matrix row that owns the observation record. Under **AK1** and **AK5** the
recipient `unseen` route publishes the refused-frame reference rather than a reason and a frame kind,
and under **AK6** the latch section publishes the terminal-frame reference, which is `C4-P2`'s second
conjunct's other precedence operand and had no publishing artifact at all.

That AK1 sentence was false when it was written, and **AL2** is the correction. The recipient `unseen`
route is two cells and the prose beneath them; the AK1 pass changed the prose and left both cells
publishing a provenance, a detailed reason, a bare frame kind and an effect certainty — the pre-AK1
record, from which a vector carries no session and takes `C4-P2` red on the conforming two-session
vector AK1 was raised for. Both cells now publish the refused-frame reference in the same five-field
form as the other four surfaces, and the design verifier registers the cells as surfaces of their own
rather than accepting one publication anywhere inside the route.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

Added for D1-D4; T3/R1/R3/S1/S2/U8/W4/X1/X2/X5/Z2 corrected; 108 cells enumerated independently, none empty; carries the ordering fact C4 owns; AE2 corrected; unchanged by AF and AG; unchanged by AH; AJ1 corrected - AI1 changed the settling-frame reference this grid publishes and reached the brief and the machine only, and AJ6 replaced the positional argument for the arrival ordinal; AK1/AK5/AK6 corrected - the recipient `unseen` route publishes the refused-frame reference and the latch section publishes the terminal-frame reference; **AL2** corrected - that correction reached the route's prose and not its two `unseen` cells, which still published the pre-AK1 record, and both cells now publish the reference in the same five-field form; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Responsibility matrix

[Brontide-Channel-0.2-Responsibility-Matrix-0.1.md](../Brontide-Channel-0.2-Responsibility-Matrix-0.1.md)

Status: proposed first-batch ownership contract; B3 and cross-artifact N1 corrected after independent
review and confirmed unchanged by the fourth, fifth, sixth, and seventh reviews, then corrected for
S1, then for U2, then for AB2, then for AC1, then for AJ1. Under AC1 that row's crossing artifact
carries the settling frame's arrival ordinal and the refused frame's kind, and under AJ1 its session,
so the fact this matrix owns and the fact the parity profile compares are the same fact. Under AB2 the matrix owns local observation content: `C4-P2` reads the
observation record, the matrix already owned the observability system that consumes it, and a fact a
property depends on with no owner row is the defect S1 was raised for. Its ordering row was the
evidence for S1: the fact the `unseen` cancellation verdict
depends on had no owner. Under the 2026-08-13 S1 ruling the delivery row is scoped to cross-interaction
ordering and a new `Intra-interaction frame order` row assigns that fact to `channel`, carried
by a per-interaction frame order declaration in the realization profile. U2 closed the owner
vocabulary: that row first used `channel-core`, a second identifier for the contract family every
other Channel-core row already called `channel`, and the identifiers are now declared once and used
only from that list. It is subject to a fresh independent closure re-review. Under **AJ1** the
`Local observation content and provenance` row's crossing artifact carries the settling frame's
**session** alongside its arrival ordinal, in the same form every other publishing artifact uses. This
status block previously declared the artifact unchanged by every pass through AI9 while the sentence
above it asserted that the fact this matrix owns and the fact the parity profile compares are the same
fact; AI1 added the session to the parity profile and not to this row, so that sentence was false at
the pin the fourteenth review assessed. Under **AK1**, **AK5** and **AK6** that row's crossing
artifact carries two further frame references in the same five-field form: the frame refused where a
refusal opens no interaction, which this row published as a kind and a provenance while `C4-P2`'s
first conjunct scoped its membership test to a session the record did not name, and the frame a
terminal history was accepted on, which is the second conjunct's other precedence operand and which
this row — the row that owns the observation record — did not carry at all. Unchanged by
**AL1**-**AL4**: this row is one of the five surfaces publishing the refused-frame reference and it
publishes the whole list, which the sixteenth review verified surface by surface; the surface **AL2**
found short is the state/event grid's two `unseen` cells.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

B3/N1/S1/U2 corrected; `Intra-interaction frame order` added, owned by `channel`, and the owner-identifier vocabulary is now closed; unchanged by the AE, AF, and AG passes; unchanged by AH; AJ1 corrected - the crossing artifact of the row that owns the observation record carries the settling frame's session, without which the fact this matrix owns and the fact the parity profile compares were different facts; AK1/AK5/AK6 corrected in the same row, which published the refused frame as a kind and a provenance and did not carry the terminal frame at all; unchanged by AL, whose refused-frame finding is against the state/event grid's cells and not against this row; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Contract-completeness review

[Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md](../Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md)

Status: author pass plus B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1/U4/U5/U7/W3, X7, and
AC1-AC4 correction passes complete. The per-capability property audit now registers `C4-P2` and the mutation that must
fail it; its silence is why an unfalsifiable property survived the correction that introduced it. The
disposition history now runs to the sixteenth cycle rather than stopping at the fifth — the count is
this block's own claim about this document and went five cycles stale behind a family token, which is
**AJ4** — and the in-flight
bound's direction scope is recorded as session-wide-as-written against per-direction-as-enforced
rather than as undeclared. Under **AK2** the Channel index's Design reviews row names the `W` family
this document's sibling record is named after; under **AK3** the counts of properties here are counts
of properties rather than of audit rows, the package stating twenty-six and the audit covering
thirteen capability-wide ones in twelve rows; and under **AK1**, **AK5**, **AK6**, **AK7** and **AK8**
this document carries the complete enumeration of every operand `C4-P1` and `C4-P2` read, which is the
durable half of the audit those five findings came from. Under **AL1**, **AL2**, **AL3** and **AL4**
the enumeration carries the `session state` operand that audit's own trigger set could not see, the
`S3` and `S5` audit rows carry the session scopes their properties lacked, and the refused-frame
reference's row records that a publishing surface named at the granularity of a route is satisfied by
one passage inside it — which is how that row read `sufficient` while two cells on the route it named
published the pre-AK1 record.
A fresh independent closure re-review remains required. This review asks what the proposed contract
does not say. It is separate from conformance review and does not claim the contract is correct. Under **AI3** the `I5` row carries the AE3 connection its `C4` sibling already had, and the pointer added with it names its direction correctly.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

All findings through T1-T4, R1-R3, S1-S3, U1-U8, and the V-Z iteration families corrected and dispositioned; AE3/AF7 corrected, and AG1 closed the silence-probe row AF1's evidence named second; AH2 and AH5 corrected; AI3 corrected; AJ4 corrected - the status block said its disposition history runs to the eighth cycle while the history ran to the thirteenth, and it now runs to the fifteenth; AK3 corrected - its property counts are counts of properties rather than of audit rows; and AK1/AK5/AK6/AK7/AK8 are recorded here with the complete `C4-P1`/`C4-P2` operand enumeration the audit produced; **AL1**-**AL4** corrected - the enumeration carries the `session state` operand AL3 found missing, the `S3` and `S5` audit rows carry their scopes, and the disposition history runs to the sixteenth cycle; **AR1** corrected - the per-capability audit described `C5-P1` and `C6-P1` by their first clause alone and named one mutation each, and both mutations fire through that first clause: the second clause of each had no input that reached it and could be deleted from the evaluator with both gates green. Each property now names its two clauses and carries a mutation per clause, on the rule the C4 row already states; **AT1**-**AT7** corrected - that correction closed its class with a gate rule keyed on properties which declare a conjunct, so it could not reach `I4`, which declares none and kept two clauses against one mutation; `C6-P1`'s second clause requires three things of a denial and only the first had an input; and `C10-P1` reads the refusal before the terminal histories and returns there. The audit rows for `I4`, `C6` and `C10` now name a mutation per clause and, for `C6-P1`'s second clause, one per obligation, and the class is closed one level lower than AR1 closed it -- the coverage gate fails when an operand of an evaluated expression is never evaluated, which is structural over every property whatever its artifact calls the clauses; **AU1**-**AU5** corrected - that class arrived a third time, on eleven obligations across nine properties which are evaluated on every declared input and never fire, including the second obligation inside the very clause AR1 was raised against, and each was deletable outright with every gate green. Nine audit rows now name a mutation per obligation, the property gate fails when any verdict-constructor call site is reached by no declared input, and AU2 corrects six properties that could not tell a violation from a vector omitting the field they read; AU5 is this index's own row for the redesign plan, which carried the AT clause twice

## 0.1-to-0.2 migration ledger

[Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md](../Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md)

Status: proposed first-batch migration disposition; B4, N1/N3, F3, D5, T1/T2, S1, Z4, and AC2 corrected
after independent review and subject to a fresh independent closure re-review. Under AC2 the closed
detailed-reason set for `invalid-interaction-correlation` carries `unopened-interaction-identity`: the
five identity reasons covered no refusal of a control naming an identity that was never opened, which
is the reason `C4-P2`'s first conjunct quantifies over and the parity profile compares. Under Z4 the
new-evidence inventory carries intra-interaction frame order, its two ordering mutations, and the
observation fields they compare, none of which has a Channel 0.1 predecessor to migrate from.
Serialized spellings remain unselected until the neutral contract batch. Under **AJ1** the
new-evidence inventory states the settling-frame reference in the same form as the four other
artifacts that publish it — five other lists, because the brief publishes it twice — including its
**session**: this inventory is what Batch 2 builds its vector
groups from, so a reference stated here with fewer fields than the parity profile compares is a vector
group authored against the wrong observation. That count of artifacts was written as five until
**AK4**, which matters only because the number of artifacts publishing this reference is the exact
quantity AJ1 turned on. It is otherwise unchanged by the **AI1**-**AI9** and
**AJ1**-**AJ7** families. Under **AK1**, **AK5** and **AK6** the inventory adds the refused-frame and
terminal-frame references those vectors compare, in the same form and for the same reason. Unchanged
by **AL1**-**AL4**, which changed no reference's field list, added no evidence requirement, and
dispositioned no further 0.1 case: their subject is where the existing fields are published and which
session a property means.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

B4/N1/N3/F3/D5/T1/T2/S1/Z4 corrected; the ordering non-promise is **replaced**, and the new-evidence inventory carries the ordering mutations; AE5/AF3/AF4 corrected; unchanged by AG; unchanged by AH; AJ1 corrected - the inventory Batch 2 builds vector groups from states the settling-frame reference in the same form as the four other publishing artifacts, five other lists; AK4 corrected - the status block counted those lists as artifacts; AK1/AK5/AK6 corrected - the inventory adds the refused-frame and terminal-frame references the same vectors compare; unchanged by AL, which changed no reference's field list and added no evidence requirement; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Neutral contract and vector brief

[Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md](../Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md)

Status: proposed first-batch artifact boundary; no neutral schemas or generated code exist yet, and
subject to a fresh independent closure re-review. Batch 2 opens only after that review conforms and
its closure record exists. Under AC2 the parity profile names the detailed reason
`unopened-interaction-identity` instead of describing it, and compares the kind of frame refused where
a refusal opens no interaction. U3, V1, V2, W1, W2, W5, W6, X1, X2, X4, Y1, Y4, Z1, and AC2 corrected after
independent review, the last restricting that ordinal to identification so the property language does
not regain the observed arrival order W1 removed from it: the parity profile compares the frame a late-traffic latch settled against rather
than only the latch value, that reference carries the settling frame's arrival ordinal so a duplicate
terminal cannot be mistaken for a reordering, the local-observation schema has positions for both, the
latch's `not-applicable` value is compared rather than absent, the required
adversarial groups carry one ordering mutation per `C4-P2` conjunct, and the
property operator set gained the one bounded precedence relation `C4-P2` needs, stimulus steps name
their committing endpoint and their session so that relation has its operands — the session under AH1,
which this sentence described in its pre-AH1 form until **AJ4** — the parity profile compares the
late-traffic latch the grid already required as evidence, and the established-profile
image carries the realization's per-interaction frame order declaration, the required adversarial
groups include one owning intra-interaction frame order and its ordering mutation, the parity profile
compares the peer-fault detailed reason, and the neutral provider may inject deterministic
per-interaction reordering so a declared ordering mutation can actually be executed. Under **AI1** the
settling-frame reference carries its session, and under **AI7** the established-profile digest is
compared per session the vector carries — the same list AH1 made per-session having left both
singular. Under **AJ1** this artifact's two field lists publish that reference in the one form the
interaction machine, the state/event grid, the responsibility matrix, and the migration ledger use;
under **AJ3** the vector format's own profile entry is inside the per-session distribution rather than
separated from it by a comma, which is AI7's second named entry; and under **AJ4** this block describes
the declared stimulus step in its post-AH1 form. Under **AK1**, **AK5** and **AK6** the
local-observation schema and the parity profile carry two further frame references in the same
five-field form — the frame refused where a refusal opens no interaction, which is `C4-P2`'s first
conjunct's operand and carried neither AF8's session nor the identity nor the committing endpoint, and
the frame a terminal history was accepted on, which is the second conjunct's other precedence operand
and was published by no artifact. Unchanged by **AL1**-**AL4** as to its own content, and load-bearing
for **AL3**: this brief's vector format is what a vector distributes per session, so C12's declared
list of facts a vector may hold more than one of is now checked against this artifact rather than
against the members that were visible when that list was written.

**As the Channel index recorded it.** Moved verbatim from that index's table row under W3; the row
now points here.

Author pass plus U3/V1/V2/W1/W2/W5/W6/X1/X2/X4/Y1/Y4/Z1 corrections; property operators, vector format, parity profile, and provider boundary now carry what `C4-P2` needs; AE1/AE3/AF5 corrected, and AG2 added the session qualifier to the precedence relation; AH1 and AH6 corrected; AI1, AI5 and AI7 corrected; AJ1, AJ3 and AJ4 corrected - both field lists in the one published form, the vector format's profile inside the per-session distribution, and the status block describing the declared stimulus step in its post-AH1 form; AK1, AK5 and AK6 corrected - the local-observation schema and the parity profile carry the refused-frame and terminal-frame references in the same five-field form; unchanged by AL, whose AL3 correction reads this brief's vector format as the authority C12's declared fact list is now checked against; unchanged by **AR1**, which is about which inputs the property gate runs rather than about any design artifact's text, and reached only the per-capability property audit; unchanged by **AT1**-**AT7** for the same reason, which found three further clauses no input reached and reached the same audit; unchanged by **AU1**-**AU5** for the same reason, which found eleven obligations no input reached and reached the same audit

## Channel index rows with no design artifact

The Channel index carries two rows that name no first-batch design artifact. Their disposition text
is moved here for the same reason as the rest, and the rows keep only what they are for.

### Design reviews

16 retained attestations, fifteen `does-not-conform` and one `conforms-with-nonblocking-findings`, the seventh the first with complete isolation, plus 11 iteration reviews recording the author-side V, W, X, Y, Z, AA, AB, AC, AD, AM, AN, AO, AP, AQ, AR, and AS passes — each family named rather than compressed to a range, because AE4 was this row omitting AA and AB behind "V-Z", and AK2 was it omitting W, the family the retained record is named after, whose findings that record keeps in a table rather than under the headings the AE4 check derived its class from; **AL1**-**AL4** retained, and the dispatch provenance of closure review 15 written, which the review 15 entry had promised and this file did not carry

### Verification foundation plan

Added in the **AL1**-**AL4** pass and adopted 2026-08-17; not a design artifact, not part of the reviewed package, and assessed by no closure review, so the AL family reaches it as its occasion rather than as a correction to it. Records the owner decision holding the next closure review, the five causes behind sixteen cycles of findings, four ranked work items with acceptance criteria — one owning artifact per fact, properties that execute, disposition history out of the status blocks, and the closure gate as an owner call — the four conditions that end the hold, and three open questions. Section 2a records what W2 has landed: `C4-P2` executes in `build/verify-channel-0.2-properties.ps1` over the eleven inputs closure review 16 evaluated by hand, with nine operand mutations reproducing that review's P3 table, so the property eight families were about is now falsified in the gate on every commit rather than by whichever reviewer rebuilds the evaluator. Section 2c records W1 for the three frame references: the field list is owned by `conformance/channel-0.2-facts.json` and rendered into every publication site, and the frame-reference registry is deleted rather than extended. It then records W1 for the recipient `unseen` refusal record, which is declared as one fact with the refused-frame reference nested inside it and rendered into its five surfaces, retiring the design verifier's AL2 sweep in favour of one keyed to the declaration and carrying no neighbour exemption; that completes the first of the four conditions that end the hold. Sections 2d through 2j record seven condition-4 passes and own the dispositions of the verification families **AM**, **AN**, **AO**, **AP**, **AQ**, and **AS**; **AR** is a design family whose disposition lives in the completeness review, while this plan records the coverage instrument that found it. None of the seven passes met condition 4. The AS pass retained five probes for character-bounded negative or recognizer extents, corrected one clean-package false positive exposed by the broader recognizer, and added a harness self-check after a transient restore failure left B7's mutation in the plan; it leaves the rest of the guard harness, coverage gate, and compound-condition operands as the eighth pass's named scope
