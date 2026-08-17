# Channel 0.2 design-foundation closure review 15 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-15-2026-08-16-5cfa5ed`

Reviewed commit: `5cfa5ed71836082f0fb97e1be1873e49acde759d`

Date: 2026-08-16

Overall verdict: **`does-not-conform`** — one blocking finding (**AK1**) and three nonblocking findings
(**AK2**-**AK4**).

**AJ1 is closed, and I confirmed it by evaluator rather than by reading.** All six surfaces that
publish the settling-frame reference now carry the identical five fields in the identical order, and
probe **P3** reproduces AI1's exact false green on `C4-outcome-precedes-ack` from the pre-AI1 four-field
form and a correct red from the form published at this pin. **AJ2**-**AJ7** are closed in the artifacts
their own evidence sections name and quote, each re-derived from those sentences rather than from an
index. The AJ1 replacement check is a real improvement over the one it replaces and it is falsifiable:
probe **P5** breaks it two ways and watches it fire.

**AK1 is the same class one operand over, and it is in the conjunct nobody has audited this way.**
Every cycle from AI1 through AJ1 has been about `C4-P2`'s **second** conjunct's operand — the
settling-frame reference — being published without the session AH1 made necessary. `C4-P2`'s **first**
conjunct has an operand too: the recipient's recorded `rejected-protocol` refusal at `unseen`. Five
surfaces publish what that record contains, and **not one of them states that it carries the session or
the interaction identity**. C4 nevertheless scopes the conjunct's membership test to "the identities the
recipient admits **in the same session**" — AF8's correction — and the redesign plan's dated AE1 ruling
carries the same narrowing. The qualifier has no operand. Probe **P4** builds the two-session vector
AF8's own text names as the failure it exists to prevent, and `C4-P2` goes **red on behaviour that is
conforming at both endpoints in both sessions**; it goes green the moment the refusal record publishes
its session. That is AE1's defect — a property that cannot stay green — restored through the operand
AF8 scoped but nothing carries, which is W5's shape ("an operator whose operand does not exist") and
AH1's shape ("scoped the relation to a session and left the step unable to say which one").

Every retained finding B1 through AJ7 is closed in the artifact it was raised against, verified against
each finding's own evidence sentences and not against any index. Nothing in this cycle repeats the
AI9 class.

## Isolation

Complete, with the dispatch provenance disclosed in its own section below.

```text
C:/b035  ->  5cfa5ed71836082f0fb97e1be1873e49acde759d  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | wc -l     ->  892
git diff HEAD --stat     ->  (empty, 0 lines)
```

The clone materialised completely: 892 tracked paths, clean status, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was read
from `C:/b035`, and all four gates available to this review were run there. **The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at any
point in this session.**

The reviewer identity above differs from all fourteen retained reviewers, from every correction author,
and from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md`
and `docs/future/channel/reviews/README.md` were both read from the clone at the pin and are the source
of this review's scope; the dispatching message was not.

Two mutation tests (probe **P5**) required temporary edits to one tracked file inside my own clone.
Both were reverted with `git checkout -- .` in the same command, and `git status --porcelain` was empty
afterwards, which is the line shown above. **Nothing is committed and the clone is clean.** The
`C4-P2` evaluators used in probes **P3** and **P4** import no repository code; they were written from
the published prose of C4, C10, the brief's operator set, vector format and parity profile, and the
latch and `unseen` sections of the interaction machine and the grid, and they live outside the clone.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect and no area of
suspicion. Four things in it narrowed *where* effort went, and I record them so the next cycle can
discount accordingly.

1. It told me to verify the pin myself rather than take it from the brief, and named U6, X6, and AI8 as
   the reason. I did — see **Pin** — and it holds in the subject, the date, and the tree-hash form.
2. It restated the policy's requirement of at least one genuine attempt to falsify a capability-wide
   property. Roughly half the effort here went to C4, C10, C12, the neutral brief, the interaction
   machine, the grid, and the matrix. C1, C3, C5, C6, C7, C9, and C11 were assessed by reading and
   cross-tracing, with falsification attempts at `C2-P1`, `C9-P1`, and `C10-P1` (probe **P7**, all three
   negative).
3. It told me to verify "every retained finding is closed" against the findings' own evidence sections
   rather than against the indexes asserting it, and named AI9 as the reason. I did; that instruction
   produced no finding this cycle, because the AJ pass did close every AJ finding in the sentences its
   evidence quoted. I record the negative result because it is the first cycle in eight where it is
   negative.
4. It told me that where a finding's correction propagated a fact across artifacts I should check every
   artifact stating that fact, not only the ones the finding named, and that the previous cycle turned
   on a reference published in five places and corrected in three. That instruction pointed me straight
   at the settling-frame reference, and I verified all six surfaces. **It did not point me at AK1.**
   AK1 is a *different* reference — the `unseen` refusal record — and I reached it by asking what
   `C4-P2`'s **other** conjunct reads and where that is published, which is a question the brief did
   not ask and no retained record has asked.

**Did anything in the dispatch narrow what I concluded?** No. The brief conveyed no defect, no
suspicion, and no conclusion, and it told me explicitly that manufacturing a finding to avoid a
conforming verdict and suppressing one to reach a cleaner verdict are equally failures. The blocking
finding sits inside the property the dispatching author has now corrected across six consecutive
families, on the one operand of it that no correction, no check, and no retained attestation has ever
opened. I record below the one respect in which the brief's framing plausibly helped me find it: it
told me to look at propagation of a fact, and asking "which facts does this property read, and where is
each published?" is the natural generalisation of that instruction. The generalisation is mine; the
instruction stopped at the settling frame.

## Disclosed process deviation and dispatch provenance

This section records, verbatim in substance, what the dispatching session disclosed to me. All of it is
true on that session's own account and I have verified the parts that are checkable from the repository.

- **The commit under review, `fix(channel): close AJ1-AJ7`, was written by an actor with no prior
  history of this work.** It was spawned specifically because the previous correction author had
  produced the same class of incomplete correction in **seven consecutive cycles**. The commit message
  at `adae88d` states this itself, and the author line differs (`niizuki` rather than `Niizuki`, with
  the co-author trailer retained).
- **The session that dispatched this review is that previous correction author.** It authored the AD
  through AI corrections and the AD correction iteration review; it dispatched closure reviews 9
  through 14; it recommended the 2026-08-15 closure-standard ruling **after the twelfth review's
  verdict was known**; and it verified and committed the AJ correction it did not write. This is a
  materially different relationship from the one disclosed for reviews 10 through 14 — the dispatcher
  is no longer the author of the commit under review — but it remains the author of everything the
  commit corrects and the party whose seven-cycle failure record occasioned the change of actor.
- **That session's verification of the AJ commit consisted of**: opening the five settling-frame
  surfaces, confirming the escape-clause regex empirically, reading the AJ4 sentences in place, and
  running the gates. It conveyed **none** of its findings, reasoning, or conclusions to me.
- The brief named **no artifact defect and no area of suspicion**, and told me I was reviewing work
  arranged by a party with an interest in it closing, and that this was a reason to probe harder rather
  than to defer.

**What that arrangement means for this verdict.** The dispatcher's disclosed verification covers
exactly the ground where I found nothing: the five settling-frame surfaces are correct, the escape-clause
regex behaves as claimed, the AJ4 sentences are rewritten in place, and the gates pass. The blocking
finding is outside all four of those, and the dispatcher had every opportunity and every incentive to
reach it — AK1 lives in the same property, in the conjunct beside the one it has corrected six times.
That is evidence the cold context did its own work; it is evidence and not proof, and the next cycle
should weigh it as such. It is also worth stating that the standard in force means any finding at all
withholds closure, so there was no verdict available to me that would have been softened by
under-rating AK1.

**Whether the dispatch narrowed where I looked**: yes, as itemised in the caveat above — the pin, the
property evaluator, the retained-finding re-derivation, and the propagation sweep. Three of the four
produced nothing. The fourth produced confirmation that AJ1 is closed.

## Pin

The policy's pin clause (`docs/future/channel/reviews/README.md`, lines 682-693) names the current
target as the commit titled `fix(channel): close AJ1-AJ7`, "committed 2026-08-16", and instructs the
reviewer to review "that commit or any later commit whose design artifacts hash identically to it — and
check that claim rather than assuming it", citing U6, X6, and AI8.

I checked it against the repository rather than against the clause's own wording, and it holds in all
three forms:

```text
git log -1 --format=%s adae88d              ->  fix(channel): close AJ1-AJ7
git log -1 --format=%ad --date=short adae88d->  2026-08-16   (clause says "committed 2026-08-16")
git rev-parse 5cfa5ed^{tree}                ->  652b60eaa71a146a81d1e518092e304b922f8fe0
git rev-parse adae88d^{tree}                ->  652b60eaa71a146a81d1e518092e304b922f8fe0
git diff --stat 5cfa5ed adae88d             ->  (empty)
git log -1 --format=%P 5cfa5ed              ->  6cddb99 adae88d
```

The reviewed tree is **byte-identical** to the tree of the commit the clause names — not merely
identical in the design-artifact pathspec, which is the weaker form earlier reviews had to settle for.
`5cfa5ed` is the merge of PR #125 and `adae88d` is its second parent and the head of the correction
sequence beginning at `fix(channel): make C4-P2 falsifiable`. **U6, X6, and AI8 are closed at this pin.**

## Blocking findings

### AK1 — `C4-P2`'s first conjunct reads a record that publishes neither its session nor its interaction identity, so AF8's session qualifier has no operand and the property is red on a conforming two-session vector

**Artifacts.**
`Brontide-Channel-0.2-Capability-Contract-0.1.md` — `C4-P2` (lines 265-273), the membership-scope
paragraph (lines 298-307), the required-green set (lines 309-315), and C10's two enumerations
(lines 536-543 and 545-553);
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` — the `unseen` recipient transition row
(line 103) and the terminal-provenance table's last row (line 270);
`Brontide-Channel-0.2-State-Event-Coverage-0.1.md` — the two `unseen` cells (line 82) and the prose
beneath them (lines 117-124);
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` — the local-observation schema (lines 176-187),
the parity profile's detailed-reason, refused-frame-kind and subsequent-admission entries
(lines 342-350 and 373-388), and the exclusion list (lines 392-401);
`Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` — the 2026-08-14 AE1 ruling and its AF8
narrowing (lines 596-623).

**The claim the design makes.** C4, lines 298-307:

> The conjunct reads **the recipient's subsequent admission of the refused identity** … through a
> membership test over the identities the recipient admits **in the same session**. The scope is the
> session and not the vector: an interaction identity is unique within a session and a new session may
> legitimately reuse the value, so a two-session vector could otherwise hold one identity refused at
> `unseen` in one session and admitted in another, satisfy the test across them, and take the conjunct
> red on conforming behaviour — AE1's own failure mode reached through the operand's scope instead of
> through a missing clause. That is AF8 …

**What the artifacts actually publish.** The record the conjunct quantifies over is the recipient's one
local observation of the refusal. Five surfaces state its contents and they agree with each other
exactly:

| Surface | Published contents of the `unseen` refusal record |
| --- | --- |
| C10, lines 545-553 | "the refusal, **the kind of frame refused**, and its provenance with the detailed reason `unopened-interaction-identity`" |
| Interaction machine, line 103 | "record one local observation carrying that reason, the kind of frame refused, and effect certainty `known-none`" |
| Grid, line 82 (both `unseen` cells) | "`rejected-protocol` provenance, detailed reason `unopened-interaction-identity`, frame kind …, and effect certainty `known-none`" |
| Grid prose, lines 117-124 | "including the detailed reason `unopened-interaction-identity` and the kind of frame refused" |
| Brief parity profile, lines 342-350 | the peer-fault detailed reason, and "the kind of frame refused where that refusal opens no interaction" |

**Neither the session nor the interaction identity appears in any of the five.** The brief's
local-observation schema (lines 176-187) enumerates "local provenance, state, admission decisions,
dispatch boundary, terminal form, detection point, the late-traffic latch with the frame that settled
it, and effect certainty" — and gives the settling frame a five-field position while giving the refusal
none. C10's general sentence, which *does* require observations "sufficient to distinguish profile,
session and interaction identities", is scoped to "**Every attempted establishment and interaction**",
and C10's very next paragraph places this record outside that class in terms: "is **neither** an
attempted establishment **nor** an attempted interaction — under C4 no interaction exists there". The
record's own sentence then ends "it retains no interaction state, because there is none to retain",
which argues *against* the missing fields rather than merely omitting them.

**Probe P4 reproduces the failure.** Two sessions in one vector — legal since AH1 and restated in the
brief's vector format at lines 208-220 — reusing interaction identity value `K`, which C4's Common terms
(lines 66-67) expressly permit. Both sessions are conforming at both endpoints:

- **s1**: the initiator commits `request(K)`; the transport **loses** it; the initiator legally commits
  its one cancellation control, because C8 states recipient admission is not observable from
  `dispatched`; the control lands at `unseen` and the recipient records the refusal. Nothing is ever
  admitted for `K` in s1. *This is the required-green member the contract names at line 312 and the one
  AE1 was raised for.*
- **s2**: the initiator commits `request(K)`; it is delivered; the recipient admits it; the initiator
  then commits its one cancellation control, which correlates normally. No refusal at `unseen` in s2.

Evaluating `C4-P2`'s first conjunct from the published prose, over the published fields:

```text
published refusal-record fields only  ->  RED  [conjunct1: session=s2 identity=K]
as if the record published its session ->  green
```

Both sessions satisfy the conjunct's precedence half — each has the initiator committing `request(K)`
before `cancellation-control(K)` — so the precedence half cannot bind the record to s1 rather than s2.
The published fields cannot either: provenance, detailed reason, refused frame kind and effect
certainty are identical in both. The membership test then finds `K` in the admitted set and the property
goes **red on conforming behaviour**. This is exactly the sentence C4 writes at line 304 to say what
must not happen, and exactly the failure AF8 was raised to fix.

**Why the obvious counterarguments do not hold.**

- *"The observation obviously belongs to a session; it is recorded inside one."* This is the argument
  AI1 rejected for the settling frame — "without the session two steps in different sessions match
  every other published field" (interaction machine, lines 18-21). The standard this design has set,
  three families running, is **publication**, not inferability. Applying a weaker standard to conjunct 1
  than to conjunct 2 is the asymmetry AK1 names.
- *"The peer fault carries session identity, so the fact exists."* It does — the brief's
  peer-protocol-fault schema, lines 170-172. But `C4-P2` says "no endpoint **records**", and C4's X5
  passage is explicit that what it reads is the observation: "It does **record** one C10 local
  observation of the refusal … `C4-P2`'s first conjunct still has the recorded refusal it quantifies
  over" (lines 244-251). The parity profile compares observations, and it compares the refusal's
  provenance, detailed reason and frame kind — not its session or its identity.
- *"Identity values are compared anyway."* The brief's exclusion list (lines 392-401) excludes "opaque
  generated identity values" by default and compares "session and interaction identity **spaces**
  (shape/scope, not opaque values across runs)". The membership test is intra-run so it is reachable,
  but nothing in the parity profile names the refusal record's own session or identity as compared
  fields, which is the Y1 condition — a property reading fields no observation is required to hold.

**Why no gate sees it.** The AJ1 check (verifier lines 1700-1763) is written over the *settling-frame*
reference and is exact and total over it. There is no corresponding check for the refusal record: the
AC2 check reaches the detailed reason, AE2 the effect certainty, and AC2 again the refused frame kind —
one field per finding, each added by the pass that needed it, and no check over the record as a whole.
That is the structural half of this finding and the reason it survived: the settling-frame reference
acquired a class check in this very commit, and the other operand of the same property still has none.

**Blocking**, on the programme's own precedent. AE1 was blocking for `C4-P2` being red on a conforming
realization; AI1 and AJ1 were blocking for an operand of `C4-P2` losing its mapping under multi-session
vectors. AK1 is both at once, and it lands in the artifact set Batch 2 authors
`capability-properties.json` and its C4 vector group from.

## Nonblocking findings

### AK2 — the Channel index's Design reviews row omits the `W` family, which the policy's own provenance table and roster declare a retained iteration review records

**Artifacts.** `docs/future/channel/README.md`, the Design reviews row, line 68;
`docs/future/channel/reviews/README.md`, the finding-family provenance table, lines 131-155, and the
retained-iteration-review roster, lines 704-720;
`channel-0.2-w-correction-iteration-review.md`, §"Why this record exists at all" (lines 19-24) and
§"Disposition of W1-W6, recorded here because their pass did not record it" (lines 358-367);
`build/verify-channel-0.2-design.ps1`, the AE4 check at lines 1254-1264 and the `$recordedFamilies`
derivation at lines 1154-1178.

The row reads:

> … plus 4 iteration reviews recording the author-side **V, X, Y, Z, AA, AB, AC, and AD** passes — each
> family named rather than compressed to a range, because AE4 was this row omitting AA and AB behind
> "V-Z"

The policy's provenance table classifies **nine** families as `iteration`: V, **W**, X, Y, Z, AA, AB,
AC, AD. Its roster entry for the W correction iteration review says that document "is also the retained
record for the two W passes, which left none of their own — that gap is X7", and the review itself
carries a dedicated `## Disposition of W1-W6` section with a row per finding. The row names eight of the
nine. **`W` is the family the retained record is named after, and it is the one the row drops.**

AE4's requirement over this row is that it "names every family the retained iteration reviews record,
spelled out rather than compressed to a range". It has not met that requirement since AE4's own
correction — six cycles — and the row's own text cites AE4 while committing AE4's defect against a
different family.

**Why no gate sees it.** The AE4 check iterates `$recordedFamilies`, which is derived at line 1156 from
`^### ([A-Z]{1,2})[0-9]+ ` headings inside the iteration reviews. The W review raises X, Y, Z, AA and AB
under finding headings and records W1-W6 in a **table**, so `W` never enters `$recordedFamilies` and the
check cannot ask for it. The separate provenance-table check at lines 1246-1251 does cover W, but it
tests only that *some* iteration review mentions `W[0-9]`, which the disposition table satisfies. The
two checks between them define the class correctly and neither applies it to this row.

Nonblocking, on the programme's unbroken precedent for entry-point and description staleness (S3, AA1,
AA2, AD3, AE4, AF2, AG4, AG5, AH3, AI2, AI4, AJ2, AJ4). Recorded because it is the AD3 class — three
surfaces describing one retained document and one of them understating it — in the row AE4 created to
end that class, and because AD1 is the proof that a later pass consults the description instead of the
document.

### AK3 — the package states twenty-six properties; two surfaces say twenty-five, and the property the count drops is `C4-P2`

**Artifacts.** `docs/future/channel/reviews/README.md`, the residual-work sentence, lines 34-37;
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, §"State-machine properties" opening
(lines 190-193) and the AF7 disposition (lines 538-540);
`Brontide-Channel-0.2-Capability-Contract-0.1.md`, the thirteen `**Property C<n>-P<n>.**` headings;
`Brontide-Channel-0.2-Session-State-Machine-0.1.md`, `S1`-`S6` (lines 152-157);
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, `I1`-`I7` (lines 282-290).

Counted from the artifacts: the contract states **thirteen** capability-wide properties — `C1-P1`
through `C12-P1` **plus `C4-P2`** — the session machine states six and the interaction machine seven.
**Twenty-six.**

The policy says "of the **twenty-five** properties the package states, eleven capabilities owe the
required-green set". The completeness review says "the table above covers the **twelve** capability-wide
ones" and, in AF7's own disposition, "the audit enforced it over **twelve of the twenty-five** the
package states". All three numbers are counts of *audit rows* presented as counts of *properties*: the
audit has twelve capability rows and thirteen state-machine rows, and its `C4` row carries two
properties in one cell.

There is no substantive gap — the `C4` row does audit both `C4-P1` and `C4-P2`, and `C4-P2` carries the
package's only completed required-green set — so this is a description defect, not a coverage one. The
same sentence also undercounts the residual work in the other direction: "eleven capabilities owe the
required-green set" omits that `C4`'s cell ends "`C4-P1`: **owed**", so twelve capabilities owe at least
one set, not eleven.

Nonblocking. Recorded because AJ4 and AI9 were both a document's own count being wrong about itself, and
because the property this count drops is `C4-P2` — the property fifteen cycles of this programme have
been about.

### AK4 — the migration ledger's status block counts publishing surfaces as publishing artifacts

**Artifacts.** `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, status block, lines 12-17;
`Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, latch section, lines 149-153;
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, status block, lines 26-27.

The ledger says the inventory "states the settling-frame reference in the same form as **the five other
artifacts** that publish it". Four other artifacts publish it — the interaction machine, the grid, the
brief, and the responsibility matrix — in five other *lists*, because the brief publishes it twice. The
grid's parallel sentence gets this right ("some of those **six surfaces**"), and so does the brief's
("the one form the interaction machine, the state/event grid, the responsibility matrix, and the
migration ledger use" — four).

Lowest-weight finding here; nothing about the reference itself is misstated. Recorded rather than passed
over for one reason: the count of artifacts publishing this reference is the exact quantity AJ1 turned
on ("three of the five"), a reader who takes the ledger literally will look for a sixth publishing
artifact and not find one, and AI8 established that a defect of this weight seen and dispositioned as
"noted, not raised" is not a disposition this programme's machinery can act on.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | One immutable profile established before any interaction is dispatchable; negotiated and fixed paths yield the same inspectable facts; unknown Channel versions, required features, classes, authority modes and incompatible application contracts refuse; no implicit downgrade, no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors. The established-profile image carries the realization's per-interaction frame order declaration, and W2's point that establishment verifies the declaration is *present*, never *true*, is stated at the provider boundary (brief lines 431-439). AJ3 is closed: the vector format's first bullet now reads "the established profile and initial session/interaction state of **each session the vector carries**" (line 208), so the profile is inside the per-session distribution. |
| C2 | conforms | Six states with `closed`/`faulted` terminal and non-transitioning; drain refuses new interactions while admitted ones reach a terminal fact; D1's duplicate drain is fatal with the first snapshot preserved and no interaction's certainty rewritten. `C2-P1` covers acceptance, the leave-unchanged-or-fault alternative, and terminal monotonicity. Falsification attempt **P7** failed: no session grid cell routes a terminal state to a nonterminal one, and the `closed`/`faulted` rows carry "terminal late input" or "remains `closed`/`faulted`; local observation only" in all six columns. C2's Silence disclaims reconnect, which AI5 correctly resolved by withdrawing the citation rather than editing C2 (brief lines 215-218). |
| C3 | conforms | Class, direction and external phase are three separate exact admission inputs evaluated before dispatch; `false` and `unknown` are treated identically; the receiver's independently derived phase gets D3's frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger's `state-violation` row (line 141). `C3-P1` binds all three conjunctively. The Portable Binding 0.2 profile's two declared classes match C7 and Decision 13's recorded ruling. |
| C4 | **does-not-conform** | **AK1** is blocking and is against `C4-P2`'s **first** conjunct: its operand — the recorded `unseen` refusal — publishes neither the session AF8's membership scope requires nor the interaction identity the test is over, and probe **P4** takes the property red on a conforming two-session vector. What is sound I record: probe **P3** finds `C4-P2` green on the duplicate-terminal and legal-late-control members and red on `C4-outcome-precedes-ack` under the field list published at this pin, and green on that mutation under the pre-AI1 four-field form — so **AJ1 and AI1 are closed and their reasoning holds**. `C4-P1`'s three clauses, the finite positive `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, W4's retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, AF1's complete expected-observation set, AF5's seven required-green members, AH6's coverage limit, AI6's named membership subject, and both conjuncts' restriction to one endpoint's own frames all hold. |
| C5 | conforms | Positional payload/authority classification with authority positions never projecting; parsing and structural validation before handler dispatch; no partial or oversized frame becoming a partial interaction; `known-none` on every pre-dispatch structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits tighter than the profile's must be exposed and accepted at establishment. |
| C6 | conforms | Authority evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability and Shape compatibility each explicitly disclaimed as grants; local denial emits no frame and records `known-none`; cross-trust carries attributable context and exact designations and no Capability, Constraint expression, or derivation chain. `C6-P1` requires exactly one `permitted` local decision to reach dispatch. |
| C7 | conforms | Relational initialization is an interaction class under the same machine with the exact CM3-declared edge, direction, members, Operation, Capability and input Shape; the pre-Ready window is `interconnected && !ready`; success is evidence consumed by the composition root and never a Ready fact; failure returns the actual observation to CM4 cleanup or rollback. `C7-P1`'s three clauses match Decision 13's Option B text sentence for sentence, including "the composition root may initiate on the Component's behalf" and the refusal to introduce a Component-to-Component binding. |
| C8 | conforms | One accepted terminal history from five named forms; cancellation optional with fixed meaning and exactly one control; R1's held-control rule with its four exits from `validating` (admission succeeds, admission refuses, loss, drain), each discarding the held control with no answering frame and not firing the latch; T3's `cancelled`-with-no-request contradiction routed explicitly at both endpoints. `C8-P1` admits no cancellation control, drain, timeout or protocol rejection as semantic success. |
| C9 | conforms | Four provenance forms kept distinct, with the fifth terminal-provenance row correctly marked as not a terminal history and listed anyway so the record `C4-P2` reads has a declared provenance. Unknown peer-fault categories fault locally as `unrecognized-peer-fault` with no answering fault. Falsification attempt **P7** failed: no row of the provenance table permits two forms for one terminal, and the "Local observation?" column is a per-row observation obligation rather than the local-loss provenance form. |
| C10 | **does-not-conform** | **AK1** is against C10 as much as against C4, and charging it to C4 alone would be the closed-in-the-first-artifact pattern this programme has recorded seven times. C10 owns the observation record; its second paragraph (lines 545-553) is the enumeration that omits the session and the interaction identity `C4-P2`'s first conjunct reads, and it places that record outside the class its first paragraph requires those fields of. `C10-P1`'s "complete for its provenance form" is defined *by* that enumeration, so the property is structurally unable to detect its own gap — probe **P7** confirms it cannot be falsified. What is sound holds: the `not-applicable` latch rule (X2/Z3), the settling frame under Y1, AC2's refused frame kind, the three-valued certainty form, and the exclusion of diagnostics from semantic parity. |
| C11 | conforms | Facets are exact by identity and version; unknown required facets refuse and unknown optional ones are ignored only under a declared additive-absence rule; no facet may reinterpret identities, authority, the four provenance forms, or certainty. Retry is a new identity with optional causal attribution, and the single ordering fact core owns is named as C4's intra-interaction frame order with facets permitted to strengthen and forbidden to weaken it. |
| C12 | **does-not-conform** | C12's own rule is what **AK1** violates: "every property **must not fail against a conforming realization**: it carries a named set of legal inputs it must leave green, drawn from its own required vector group" (lines 636-637). `C4-P2`'s required-green set names the lost-request member, and probe **P4** shows the property red on that member the moment the vector carries the second session AH1 declared legal. `C12-P1`'s "one deterministic expected portable observation" is also unreachable for the two mutation vectors' refusal records, whose session and identity no artifact specifies. The AE3 machinery itself is correct and is what makes the defect nameable; **AK3** is against the count of properties it is applied to, not against the rule. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | conforms | Six states, ten legal transitions, eight refused/illegal rows, a closed-world totality rule that explicitly does not override a specific nonfatal row, and `S1`-`S6`. Unchanged by the AI and AJ families and its status block now says so per family rather than per last finding, which is AJ5's correction applied where AJ5 was raised. |
| Interaction state machine | conforms | Twelve initiator and twelve recipient states, seventeen initiator and twenty-four recipient transition rows including X3's `unseen` row, W4's retention rule, Y3's provenance-not-state resolution, and the five-row terminal-provenance table. AJ1 and AJ6 are both closed here: the latch section publishes the five-field reference in the common form and its justification names the fields instead of counting them. |
| State/event totality | conforms | Independently enumerated: 108 published-row cells (3 grids × 6 rows × 6 event columns), **none empty**, over 180 underlying state/event pairs (session 6×6=36; initiator 12 states ×6=72; recipient 12 states ×6=72). This agrees with reviews 7 through 14. Every `any terminal` group expands correctly — six terminal initiator states and seven terminal recipient states — and the `unseen` route's `not-applicable` latch is asserted as a value rather than left absent. |
| Responsibility | conforms | 39 rows, each with exactly one owner identifier drawn from a closed vocabulary of 22; **no row carries an owner outside the vocabulary, no row carries two owners, and no declared identifier is unused** (mechanically enumerated, probe **P6**). The `Local observation content and provenance` row now carries the settling frame's five fields, which closes AJ1 in the artifact that owns the fact, and the U2 `channel-core` normalisation holds everywhere including the retained-as-issued S1 ruling text. |
| Completeness | conforms-with-nonblocking-findings | The silence-probe table's 27 rows each carry a contract answer and a future owner; the disposition history now runs to the fourteenth cycle in place and not by appended token, which closes AJ4; AH5's AE3 connection is on both the `C4-P1` and `I5` rows, which closes AI3. **AK3** is against this document's count of the properties its audit covers. The eleven `owed` required-green cells and the `I1`-`I7` double gap remain correctly recorded as named residual work rather than guessed sets. |
| Migration coverage | conforms-with-nonblocking-findings | All 24 predecessor vectors are dispositioned with unique `CH-nn` rows against the 24 ids in `conformance/channel-0.1-vectors.json` (mechanically cross-checked, probe **P6**); every disposition uses the declared five-value vocabulary; `CH-R10` is dispositioned explicitly and the completion check claims the register, which closes AE5 and AF3. The new-evidence inventory publishes the settling-frame reference in the common form, which closes AJ1 in the artifact Batch 2 builds vector groups from. **AK4** is against that status block's count of publishing artifacts. |
| Neutral brief / Batch 2 entry gate | does-not-conform | The gate's six conditions are correctly stated and two of them are not met at this pin: "C1-C12, both state machines, and the closed state/event grid have no unresolved internal contradiction" and "the independent design review records no blocking finding". **AK1** is a contradiction between C4's membership scope and C10's enumeration of the record that scope is over. AJ1, AJ3 and AJ4 are closed in this artifact and the two field lists match the other four surfaces exactly. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 in the redesign plan's Resolved questions
(lines 511-523) are represented consistently throughout the first-batch design. I traced each into
every artifact that carries it:

| Ruling | Represented in | Consistent |
| --- | --- | --- |
| Core concurrency and cancellation | C4 (finite positive `max-in-flight`, profile may select one), C8 (optional cancellation with fixed meaning), interaction machine §Concurrent interactions and §Cancellation, grid initiator/recipient rows, matrix `Bounded unary concurrency`/`Class-specific cancellability` → `channel-profile` and `Cancellation control and terminal meaning` → `channel`, ledger `maxConcurrentRequests` → **replaced**, "single invocation" → **replaced**, "cancellation unsupported" → **replaced** | yes |
| Session-state ownership | C2's six states, session machine §Boundary's seven explicit non-states, grid session rows, matrix §"Session state versus activation phase" with Portable Binding / Composition / Component Management owners named separately (N1), ledger `ready` → **moved** to Component Management | yes |
| Relational initialization representation | C3's two declared classes, C7 in full, interaction machine §Relational initialization, session machine's external-phase predicate block, matrix `Relational interaction declaration` → `cm3-lifecycle-contract`, ledger `Lifecycle` → **removed** with the relational stage as an interaction class | yes |
| Extension invariants | C11, matrix §"Extension hooks" five-item list, brief §"Version and establishment rule" and the facet rules, ledger `realization feature declarations` → **replaced** | yes |

The three correction rulings (2026-08-13 R1, 2026-08-13 S1, 2026-08-14 AE1) and the 2026-08-15
closure-standard ruling are each recorded as issued with their rejected options, and each carries its
later narrowing as an annotation rather than a rewrite — `channel-core` under U2 for S1, and the
vector-to-session operand scope under AF8 for AE1, which closes AG3. **I read the 2026-08-15 ruling in
full at lines 644-666 rather than taking the dispatch's summary of it.** It settles that only an
unqualified `conforms` closes the batch, and it records that it was made after the twelfth review's
verdict was known and why that ordering was accepted. It governs the consequence of my verdict and not
its content; my verdict would be `does-not-conform` under any of the three standards, because AK1 is
blocking on its own terms.

## Retained findings

Every retained finding B1 through AJ7 is closed in the artifact it was raised against. I verified this
**against each finding's own evidence sentences**, per the policy's instruction and AI9's precedent, and
not against the disposition history, the indexes, or any attestation's summary. Findings through AH6
were re-derived by spot-check against their evidence; the AI and AJ families were re-derived in full,
since they are the live layer.

| Finding | Closed | Evidence re-derived at this pin |
| --- | --- | --- |
| AI1 | yes | All six settling-frame lists carry the session; probe **P3** shows the property red on `C4-outcome-precedes-ack` under the published form and green under the four-field form |
| AI2 | yes | Both narratives — `docs/future/README.md` §Priority 1 and `docs/future/channel/README.md` — now introduce the eleventh through fourteenth reviews by ordinal with their families named; neither is a token substitution |
| AI3 | yes | Completeness review, the `I5` row (line 208), carries the AE3 exposure its `C4` sibling has |
| AI4 | yes | Both sentences AI4 quoted are rewritten in place: completeness review line 8 now reads "runs to the fourteenth cycle"; brief lines 16-17 now read "their committing endpoint **and their session**". This is AJ4's closure and I checked the quoted sentences, not the family token |
| AI5 | yes | Brief lines 215-218 withdraw the C2 reconnect citation rather than repairing it |
| AI6 | yes | Contract lines 298-300 name the membership subject explicitly |
| AI7 | yes | **Both** entries AI7 named: brief line 208 (vector format) and line 334 (parity profile digest) |
| AI8 | yes | Pin clause names `fix(channel): close AJ1-AJ7`, committed 2026-08-16; `adae88d`'s own date is 2026-08-16 |
| AI9 | yes | Plan §7.8 (lines 336-341) reports fourteen retained attestations, thirteen negative and one conforming-with-findings, and records that it reported seven for six cycles |
| AJ1 | yes | Six surfaces, identical five-field list, identical order; verified by string comparison and by probe **P3** |
| AJ2 | yes | Both narratives rewritten; the plan's false AI2 claim withdrawn in terms (lines 40-44); the Channel index plan row withdraws it too |
| AJ3 | yes | Brief line 208 |
| AJ4 | yes | Both quoted sentences, as above |
| AJ5 | yes | Verifier lines 1530-1546 read escape clauses and positive claims separately, with `(?![0-9])` on the family name and escapes stripped before the positive test |
| AJ6 | yes as to substance | Interaction machine lines 223-229 and grid lines 145-148 name the fields instead of counting them, and the verifier fires on any positional phrase (mutation-tested, probe **P5**). The AI6 line-break instance AJ6's evidence quoted as unchanged — brief lines 384-386, "The conjunct / tests membership of the identity in the / set the recipient admits" — is still unchanged. I record that as an observation rather than a finding: AJ6's claim is the positional argument, and the wider mechanical class has in fact improved sharply this cycle (two long prose lines added, against AJ6's count of five over 199 characters) |
| AJ7 | yes | Retained-attestations list runs 11, 12, 13, 14 in order |
| B1-AH6 | yes | Spot-checked against evidence: N1 (three owners named separately in the matrix ruling), R1 (held control, four exits), S1 (matrix `Intra-interaction frame order` row owned by `channel`), U1/U2/U8, W4/X3/X5/Y3 (the `unseen` route), Y4/AC1 (arrival ordinal in all owning artifacts), AE1/AE3 (subsequent admission compared; required-green a normative field), AF1/AF5/AF8, AG1/AG2/AG3, AH1/AH2/AH4/AH5/AH6 |

**AD2** was ruled a defect by review 9 and its replacement is the declared provenance table; the table is
total over every family the policy bolds and every `iteration` family has a retained record. **AK2** is
against a different surface's description of those records, not against the table.

## Probes performed

### P1 — gates, in the isolated clone

```text
build/verify-channel-0.2-design.ps1               -> exit 0
   "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction
    event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings
    resolved, and independent review still pending."
build/verify-channel-0.2-design.ps1 -NegativeProbe-> exit 1
   FAIL: ... is missing '**Property C12-P1.**'   (the in-memory C12-P1 removal, and only that)
build/verify-doc-links.ps1                        -> exit 0, 867 local links across 307 documents
build/verify-text.ps1                             -> exit 0, 886 UTF-8 files
```

All four behave exactly as the commit message claims. `build/verify-interchange.ps1` was not run: it
compiles and tests both stacks, and no artifact in this review's scope is code. **Green gates are not
evidence of conformance**, and AK1 is the fifteenth demonstration of that in this programme: every gate
above is green over it.

### P2 — independent enumeration of the state/event grid

Parsed the three grids mechanically rather than by reading: 6 rows × 6 event columns each, **108
published-row cells, zero empty**. Expanding the two `any terminal` groups gives 12 initiator and 12
recipient states, so **180 underlying state/event pairs** (36 + 72 + 72). This agrees with the
enumerations recorded by reviews 7 through 14, independently reached.

### P3 — `C4-P2` conjunct 2, and confirmation that AJ1 is closed (positive result)

Wrote an evaluator for `C4-P2`'s second conjunct from the published prose — the settling-frame reference
as a five-field key, bound to a declared stimulus step, compared through the brief's precedence relation
— and ran `C4-outcome-precedes-ack` in a two-session vector reusing one interaction identity, alongside
the duplicate-terminal and legal-late-control required-green members:

```text
field set                    C4-outcome-precedes-ack        duplicate terminal
five fields (published now)  RED  (bound to s2's ack)       green
minus session (pre-AI1 form) green (reference matched 2)    green
minus arrival ordinal        RED                            green in this vector; Y4's case is the
                                                            duplicate-terminal one, where it is required
```

The middle row is AI1's exact false green, reproduced from the field list the grid and the matrix
carried at `6cddb99`. The top row is the same vector under the list all six surfaces publish at
`5cfa5ed`. **AJ1 is closed and the correction does what it claims.**

### P4 — `C4-P2` conjunct 1, falsification of a capability-wide property (positive result — AK1)

Wrote a second evaluator for the first conjunct from C4 lines 265-273 and 298-307, with the refusal
record's fields taken from the five surfaces that publish them, and a switch to compare "published
fields only" against "as if the record also published its session and identity". Ran the required-green
lost-request member inside a two-session vector.

- **First attempt returned green.** My evaluator was resolving the record's session from the precedence
  half — only s1 had a request-before-control pair — so the ambiguity did not bite. I record the
  negative result because it bounds the finding: where only one session satisfies the precedence half,
  the conjunct is still evaluable.
- **Second attempt returned red.** Making both sessions satisfy the precedence half removes the only
  disambiguator, and the published fields cannot supply another:

```text
published refusal-record fields only    ->  RED  [conjunct1: session=s2 identity=K]
as if the record published its session  ->  green
```

Both endpoints are conforming in both sessions. This is **AK1**.

### P5 — mutation-testing the new AJ1 and AJ6 checks (both fire)

Temporary edits inside my own clone, reverted in the same command, `git status --porcelain` empty
afterwards.

- Reverting the grid's list to the pre-AJ1 four-field form fires **three** distinct failures: the
  registered-surface check, the package-wide publication-shape sweep ("4 of its five field names"), and
  the exact-count check ("publishes the field list 5 times and 6 surfaces are registered").
- Restoring the grid's positional wording ("the same reason the other three are insufficient") fires the
  AJ6 check.

Both checks are genuinely falsifiable, and the exact count rather than a lower bound is the substantive
improvement over the `Count -lt 3` guard it replaces.

### P6 — mechanical enumeration of ownership, vector coverage, and the registry pins

- Responsibility matrix: 39 rows, 22 declared owner identifiers, **0** rows with an owner outside the
  vocabulary, **0** rows with more than one owner, **0** declared-but-unused identifiers.
- Migration ledger: 24 unique `CH-nn` vector rows against the 24 vector ids in
  `conformance/channel-0.1-vectors.json`; every disposition inside the declared five-value vocabulary.
- `Brontide-Architecture-Status.json`: recomputed **all twelve** SHA-256 pins — current architecture,
  implementation baseline requirements, and both stacks' matrix, requirements, plan and ledger.
  **12 matched, 0 mismatched.** Architecture 0.8 is `Complete Draft … not ratified`, no ratified
  architecture exists, and both stacks state `Designed for: Brontide Architecture 0.8, Complete Draft,
  not ratified`. Neither stack claims any Channel 0.2 implementation, which is what the design package
  requires of them.

### P7 — falsification attempts at `C2-P1`, `C9-P1`, and `C10-P1` (three negative results)

- `C2-P1`: searched the session machine's legal, refused/illegal and totality rules and the grid's
  session rows for any route from a terminal state to a nonterminal one, or any input the tables leave
  to an implementation-selected default. None exists; the `closed`/`faulted` rows are total across all
  six event columns.
- `C9-P1`: attempted to construct a terminal selecting two provenance forms. The `unseen` refusal is the
  closest case — it produces both a peer Channel statement and a local observation — but the
  terminal-provenance table's "Local observation?" column is a per-row obligation and not the local-loss
  provenance form, and the row is explicitly marked not a terminal history. Property holds.
- `C10-P1`: attempted to find a route with a possible post-dispatch path recording `known-none`. Every
  `known-none` in the machines and the grid is pre-dispatch or on a route where no interaction exists.
  Property holds — **and that is itself the observation recorded under C10's verdict**: `C10-P1`'s
  "complete for its provenance form" is defined by C10's own enumeration, so the property is
  structurally unable to detect that the enumeration is missing the two fields `C4-P2` reads.

### P8 — upstream consistency and clone completeness

Read `Brontide-Architecture-Status.json`, Architecture 0.8's status header and its §35 direction
passage, both stacks' local targets, Decision 13 in full including its four options and its recorded
ruling, the retained Channel 0.1 design note, draft contract and requirements/risk ledger, the 24
retained vectors, and the Portable Binding neutral schemas the migration ledger inventories. Decision
13's Option B text — separate readiness from establishment, a relational stage carrying the exact
CM3-declared edge, direction, members, Operation, Capability and input Shape, refusal of anything
undeclared, ordinary traffic closed until Release, the composition root standing in for the initiating
Component, and no Component-to-Component binding — is represented in C3, C7, the interaction machine and
the matrix without addition or loss. No design artifact claims implementation, ratification, or a
schema.

## What this verdict means

The batch does not close. Under the 2026-08-15 ruling only an unqualified `conforms` would, and this
verdict is `does-not-conform` on AK1's own merits rather than on that standard.

**What is genuinely different about this cycle, and worth saying plainly.** The change of correction
actor worked on the axis it was made for. The AJ commit is the first in eight cycles where I could not
find a finding closed in the first artifact its evidence named and left open in the second: AJ2, AJ3 and
AJ4 are each closed in *both* of the surfaces their evidence quoted, the AJ1 propagation reached all six
surfaces including the one no finding had named, and the replacement check is written over the fact
rather than over an artifact list and fails an exact count rather than a lower bound. The
seven-consecutive-cycle propagation failure did not recur. Three of my four findings are description
defects of the lowest weight this programme records, and two of them predate the AJ pass by six cycles.

**What that does not mean.** AK1 is not a propagation failure and would not have been prevented by any
sweep over the fact the last correction changed. It is the older and harder class: a property reads two
operands, every cycle since AH1 has audited one of them, and the other has never been opened. The method
the policy recommends — ask what the previous fix *depends on* — points down. The question that found
AK1 points sideways: **for each fact a property reads, which artifact publishes it, and does that
publication carry every field the property's own qualifiers quantify over?** Conjunct 1's answer is that
its record is published by five surfaces, all of which agree, and all of which omit the two fields AF8's
qualifier needs.

**The specific caution for the next correction pass.** AF8, AH1, AI1 and AJ1 are four corrections that
each added a session to something, and three of them added it to an operand of `C4-P2`. The fourth — AF8
— added the *requirement* of a session without adding the session anywhere, and that is the one still
open. A pass that fixes AK1 by adding the session and the interaction identity to the `unseen` refusal
record must add them to all five surfaces that publish that record's contents, must give the record a
check written over the record rather than one field per finding, and should ask the same question of
every other fact `C4-P2` and `C4-P1` read before treating the class as closed.

**On the residual work.** Eleven capabilities' required-green sets, `C4-P1`'s, and the thirteen
state-machine properties' remain `owed`, with `I1`-`I7` owing a named mutation as well. That is
correctly recorded as named residual work rather than guessed sets, and I did not treat it as a finding.
Batch 2 cannot author `capability-properties.json` until it is stated, and AK1 means the C4 vector group
cannot be authored correctly even once it is.

## Note on the design gate

The gate results in **P1** are from before this attestation existed. I re-ran all three afterwards.
`verify-doc-links.ps1` and `verify-text.ps1` still pass (867 links across **308** documents; **887**
UTF-8 files — one more of each, this file). `verify-channel-0.2-design.ps1` fails with five messages and
nothing else, and every one is the verifier working as designed on a directory that now holds a
fifteenth attestation no index has been updated for:

```text
FAIL: … must retain exactly the review README, all fourteen retained attestations, and all four
      correction iteration reviews before the next closure review.
FAIL: The review policy's retained-attestations list gives the numbered reviews in the order
      '7,8,9,10,11,12,13,14' and the directory holds '7,8,9,10,11,12,13,14,15'. … This is AJ7.
FAIL: The Channel index's Design reviews row does not say '15 retained attestations' …
FAIL: The future-work index does not say '15 retained independent reviews' …
FAIL: 'Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md' says its disposition history runs to
      the 'fourteenth' cycle and there are 15 retained review cycles. … This is AJ4.
```

The first three are the class the ninth through fourteenth reviews each recorded. **The last two are new
this cycle and are the AJ5-era checks firing correctly on their first live opportunity**: the AJ7
ordering check and the AJ4 self-description check are both written over the claim rather than over a
string, and both detect the newly retained record without anyone editing them. That is the strongest
positive evidence I have that the AJ pass's check work is sound.

Because this verdict does not conform, the correction pass does **not** additionally have to change what
`independent review still pending` or the `awaits a fresh independent closure re-review` phrases assert.
`git status --porcelain` after writing this file lists exactly one untracked path — this attestation —
and nothing modified.

`build/verify-channel-0.2-design.ps1` is now 1882 lines and is, in my reading, the strongest artifact in
this package: the AJ1 replacement check in particular registers its surfaces explicitly, sweeps the
whole package for abbreviated publications, and compares an exact count, and it is the first check here
that would fail on a *seventh* surface appearing rather than passing over it. Two structural
observations for whoever extends it.

First, the class it now guards is "the settling-frame reference's field set drifts between the artifacts
that publish it". AK1 is the same class for a different reference, and there is no check for that one —
its fields were added one per finding by AC2, AE2 and AC2 again. A check written over *the refusal
record* rather than over its individual fields is what AK1 needs, and it is the same shape the AJ pass
just wrote for the settling frame.

Second, the AE4 check's class is derived from `### <family><n> ` headings inside the iteration reviews,
while the obligation it enforces is declared in the provenance table. Those two populations differ by
exactly one family — `W`, whose findings are recorded in a table rather than under headings — and that
difference is **AK2**. Deriving the class from the declared table, as the AD2 replacement already does
thirty lines earlier in the same file, would close it.

---

**This attestation records `does-not-conform`.** It repairs nothing; the corrections are a later
commit's work and require a fresh independent closure review under a reviewer identity distinct from all
fifteen retained reviewers and from every correction author.
