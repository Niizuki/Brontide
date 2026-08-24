# Channel 0.2 design-foundation closure review 9 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-9-2026-08-14-9408948`

Reviewed commit: `940894839e2abe9a3e54536b2ed24c1f18bf6598`

Date: 2026-08-14

Overall verdict: **`does-not-conform`** — one blocking finding (**AE1**) and four nonblocking
findings (**AE2**-**AE5**), plus a recorded verdict on the open owner call **AD2**.

Every retained finding B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1-U8, V1-V3, W1-W6, X1-X7,
Y1-Y4, Z1-Z4, AA1-AA3, AB1-AB2, AC1-AC4, and AD1/AD3 is closed in the artifacts it was raised
against. **U1 is closed as to falsifiability and not closed as to soundness.** The correction makes
`C4-P2` able to fail on its named mutation, which is what U1 asked for; it also makes the property
fail on a conforming realization exercising a vector the neutral brief requires, and the two inputs
are byte-for-byte indistinguishable to every field the property is permitted to read. A property
that returns the same verdict for its own mutation and for legal behaviour is not evidence about
either.

## Isolation

Complete, with two disclosed deviations recorded below and a third disclosed in its own section
because the policy requires process deviations to be stated rather than left implicit.

```text
C:/b029  ->  940894839e2abe9a3e54536b2ed24c1f18bf6598  (detached, clean)
git status --porcelain   ->  (empty)
git ls-files | count     ->  886
files on disk (non-.git) ->  886
git diff HEAD            ->  (empty)
```

The clone materialised completely — 886 tracked paths, 886 files on disk, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was
read from `C:/b029`; all four gates available to this review were run there. The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written, or executed against at any
point.

**Deviation 1.** `docs/future/channel/reviews/README.md` and `AGENTS.md` were read from the isolated
clone, not the working tree; the dispatching brief directed both be read first and both were
available at the pin. No navigational reading of the shared tree occurred.

**Deviation 2.** No design artifact was read outside the clone. The reviewer identity above differs
from all eight retained reviewers, from every correction author, and from every retained
iteration-review actor. No author private reasoning was available.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect and no area
of suspicion. It named the method — write a `C4-P2` evaluator from the published prose and run the
named mutations through it rather than reading the property and agreeing with it — and it restated
the policy's four sharpest questions. **AE1 is the answer to the third of those questions** ("can the
recipient's `rejected-protocol` at `unseen` be distinguished in the observation record from the other
causes of that same terminal, given C10 carries no frame-order field"), so it is not an
independently conceived finding, although the cause it identifies — legal loss of the request, which
no artifact in the package contemplates — was not named by the brief and is not among the causes the
AC2 correction enumerated. **AE2**-**AE5** were reached independently and none was named by the
brief. The brief's direction to write an evaluator did narrow where this review spent its effort:
roughly half of it went to C4, C8, C10, and the state/event grid, and correspondingly less to C5,
C6, and C11, which were assessed by reading rather than by probe.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that had itself already read the Channel 0.2 design package
and this review policy, and that authored the immediately preceding correction commit
`fix(channel): close AD1 and AD3, the retained-record descriptions` as an author-side iteration pass
(retained as the [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md)).

That session conveyed none of its findings, reasoning, or conclusions to this one. The dispatching
brief named no artifact defect and no area of suspicion, and this reviewer's context contains nothing
from that session beyond the brief itself. It is recorded here so the next cycle can weigh it, and
because the same disclosure was owed and made for the T1-T4 and U1 passes.

What this reviewer can say about the effect: the brief did narrow *where* effort went, as recorded
above, by naming a method and a set of questions. It did not narrow *what* was concluded — AE1
contradicts the immediately preceding pass's own recorded result, which reports that an evaluator
built from the contract's prose "found nothing" on the first pass. That pass ran the property
against the cases C4's narrative names as ones it must leave green. It did not run it against the
loss vector the neutral brief's required adversarial group names, which is where AE1 is.

## Pin

The policy names the current target as the commit titled
`fix(channel): close AD1 and AD3, the retained-record descriptions`, committed 2026-08-14. The
reviewed commit `9408948` carries exactly that subject and is the head of the correction sequence.
The clause "or any later commit whose design artifacts hash identically to it" is vacuously
satisfied: `9408948` is itself the named commit and no later commit exists in the clone. The X6
correction — checking this sentence against the most recent commit that changed a design artifact
rather than against its own wording — holds at this pin, and the design verifier passes.

## Blocking finding

### AE1 — `C4-P2`'s first conjunct fails on a conforming realization, and cannot be told apart from its own named mutation

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §"Property C4-P2" (first conjunct)
and §C4's mutation-vector passage; `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector
groups" (required adversarial group for intra-interaction frame order);
`Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` §"New evidence required by redesign" (final
bullet); `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` recipient transition row for
`unseen`.

**The conjunct.** As published:

> no endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation control whose
> committing endpoint had already committed the request naming that identity

**The required vector it fails on.** The brief's required adversarial group is
"intra-interaction frame order and **both** its ordering mutations: conforming commit-order delivery
in both directions, **loss of either frame**, a cancellation control for an identity the peer never
opened … and then `C4-control-precedes-request` and `C4-outcome-precedes-ack`". The migration
ledger's new-evidence inventory repeats the same list. In the initiator direction the two frames
whose order the promise governs are the request and the cancellation control, so "loss of either
frame" includes losing the request while the control is delivered.

That vector is a conforming realization throughout:

1. the initiator commits the request from `admitting` and reaches `dispatched` — the interaction
   machine's row commits "to the transport/direct seam", so the commit is local and survives whatever
   the transport does next;
2. the transport loses the request. C4-P2's own second sentence — "Loss may still drop a frame" —
   permits exactly this;
3. from `dispatched` the initiator commits its one legal cancellation control. It has no way to know
   the request was lost: C8 states that "the recipient's admission transition emits no frame and
   Channel declares no request-accepted acknowledgement";
4. the control is delivered. The recipient is at `unseen` for that identity, and the interaction
   machine's `unseen` row fires: "commit one interaction-scoped peer fault with `rejected-protocol`
   provenance and detailed reason `unopened-interaction-identity`, record one local observation
   carrying that reason and the kind of frame refused".

The recorded refusal is now precisely the witness the conjunct quantifies over, and the committing
endpoint had indeed already committed the request naming that identity. The conjunct is violated by
a realization that reordered nothing.

**The indistinguishability, which is the harder half.** This is not merely a missing loss exemption.
The loss vector and `C4-control-precedes-request` present *identical values in every field the
property is permitted to read*. Both declare the same two initiator stimulus steps in the same order
for the same identity, and both produce the same recipient observation — same provenance, same
recipient state, same detailed reason, same refused frame kind, same `not-applicable` latch. This
was probed rather than reasoned (probe P3 below): an evaluator written from the published prose,
using only the closed operator set the brief permits, returns `red` for both, and a structural
comparison of the two vectors' declared steps and recorded observations returns equal on both.

So the design has no third option. Either

- the loss vector's expected observation is "property goes red", in which case the property fails on
  legal behaviour and the brief's claim that the ordering group "is the only group whose expected
  observations include a property going red" is false of its own members; or
- the loss vector's expected observation is green, in which case no evaluation of the published
  conjunct can distinguish it from `C4-control-precedes-request`, and the mutation is green too —
  which is U1 restored, by a different route than the pronoun AC3 closed.

**Why nine passes walked past it.** Every artifact that discusses the `unseen` refusal frames its
cause the same way: "an identity the recipient has never been asked to open" (C4), "a peer naming
identities it never opens" (C4, twice), "an identity the peer never opened" (grid, interaction
machine), "a recognized control naming an identity the recipient has never accepted" (ledger,
AC2's closed-set entry). All of these describe a nonconformant peer. Not one artifact records that
the same refusal is produced by a conformant peer whose request was lost, and the AC2 correction —
which enumerated the causes of that terminal precisely in order to make them distinguishable — added
one reason covering the nonconformant case and did not reach this one. The property was then written
over that reason.

**The class.** This is the layer under U1 in the direction the sequence had not looked. U1 through
AD have asked, of each fix, *can this fail*. AE1 is what the mirrored question finds: *can this fail
when it should not*. The distinction matters because a property that cannot fail and a property that
cannot stay green are the same defect measured from opposite ends — in both cases the property's
verdict carries no information about the behaviour it names. This is a blocking finding: the whole
S1/U1 line exists to give intra-interaction frame order a property that decides it, and this one
does not decide it.

**Not repaired here**, per the policy. Recording for the correction pass only, and not as a
recommendation this review is entitled to make: the distinguishing fact exists in the design already
— in the reordering case the request is delivered afterwards and the recipient admits an interaction
for that identity, and C4 already states that "a later request bearing that identity therefore
arrives at `unseen` as any other first request does, and is admitted on its own merits", while in the
loss case no such admission ever occurs. The conjunct does not read it. Whether reading it is the
right correction, or whether the loss case wants an explicit carve-out expressible in the closed
operator set, is a design choice and not this reviewer's to make. It is worth noting that the
existing carve-out sentence "Loss may still drop a frame" is *not* expressible in that operator set:
there is no "was this frame lost" operand in the vector format or the parity profile, so an evaluator
cannot apply it. That is the W1 defect class — a property clause that cannot be written in the
declared language — surviving in the one sentence nobody re-read.

## Nonblocking findings

### AE2 — the one route the design gave its own transition row is the one route whose effect certainty nothing states

**Artifacts.** `Brontide-Channel-0.2-State-Event-Coverage-0.1.md` §"Recipient interaction coverage
grid" (both `unseen` cells) and §"Evidence required";
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` recipient transition row for `unseen`, and
§"Terminal provenance" final row; `Brontide-Channel-0.2-Capability-Contract-0.1.md` C10.

The grid requires that "Each cell asserts next state, emitted frame or no-frame decision, provenance,
effect certainty, dispatch delta, sibling delta, recorded local observation, and late-traffic latch".
C10 requires every observation to record effect certainty, and `C10-P1` requires each observation to
be "complete for its provenance form". Neither `unseen` cell states an effect certainty, the
interaction machine's `unseen` row does not, and the terminal-provenance table's final row does not.
A scan of all three artifacts for `known-none` or `certainty` within 350 characters of any occurrence
of `unseen` returns nothing.

The interaction machine's totality rule does supply certainty — "`known-none` before dispatch and
otherwise `unknown`" — but only for events it claims, and X3 and Y3 made the `unseen` refusal a
detailed row precisely so totality rule 1 selects it and rule 2 does not. The rule that would have
supplied the value is the rule the correction removed it from.

This is X2's finding applied to a second field of the same cell. X2 argued that a required field left
absent on a route that has no natural value "is the silence two independent implementations agree on
by accident: one would write `clear` and the other nothing, both defensibly". The same is true here,
and sharper, because X2 established the precedent that this route takes `not-applicable` where a
concept does not apply: one implementer will write `known-none` (nothing dispatched), another
`not-applicable` (no interaction exists to have effects), both defensibly, and `not-applicable` is
not a member of C10's closed three-value certainty set. Nonblocking because a value is derivable;
recorded because deriving it is what the grid exists to make unnecessary.

### AE3 — no artifact requires a property to stay green, which is the rule AE1 violates

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` C12 §"Failure and uncertainty";
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Capability-wide property format";
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Per-capability property audit".

C12 states "Every property must be able to fail against a named incorrect implementation." Nothing
anywhere states the converse: that a property must *not* fail against a conforming one. The brief's
property format enumerates seven required fields — id, selector, quantified facts, invariant, "one
named negative probe mutation", expected failing report, evidence modes — and none of them is a set
of inputs the property must leave green. The completeness review's audit table has two columns,
"Universal property present" and "Named mutation that must fail", and no third.

The consequence is visible in this package. The only place any green obligation is written down is
C4's own prose narrative, which names three such cases for the second conjunct ("a legal late control
that arrives after a peer's terminal, and a duplicate terminal from a nonconformant peer, must both
leave this property green") and one for the first, and the brief's vector group, which names five.
Neither is a rule, neither is machine-checkable, and neither is complete — the loss vector appears in
the brief's group without any statement of what the property must return for it, which is the gap
AE1 falls through. Ten passes have now audited these properties for falsifiability and none for
soundness, because falsifiability is the only half that was ever written down as a requirement.

### AE4 — the Channel index gives a fourth partial account of what the retained iteration reviews contain

**Artifact.** `docs/future/channel/README.md` lines 25 and 50.

The four retained iteration reviews record, by their own `###` finding headings, the families
`V`, `X`, `Y`, `Z`, `AA`, `AB`, `AC`, and `AD`. The Channel index's Design reviews row describes them
as "4 iteration reviews recording the author-side V-Z, AC, and AD passes" — omitting `AA` and `AB`.
Separately, line 25 tells the reader that the pending review is "of the correction sequence that runs
from S1 through Z4", four families short of where the sequence actually ends.

This is AD3's defect in a document AD3 did not reach. AD3 found three disagreeing accounts of the W
review — its own scope line, the policy's roster entry, and the AC residual — and the check written
for it covers exactly those two places, iterating over each retained review's scope line and its
roster entry in the policy. The Channel index is a fourth place a description of the same documents
lives, and it is the entry point AA1 was raised against. The AA1 structural check passes because it
requires only that each disposition family appear *somewhere* in the index, and `AA` and `AB` do
appear, higher up.

The families omitted are, again, exactly the two the AD correction identified as the ones a reader is
"most likely to under-read" because they live inside another review's fourth and fifth passes rather
than in files of their own. Nonblocking: no reader is sent to reconstruct evidence, as AD1's reader
would have been. Recorded because the mechanism is identical and the check written to stop it was
scoped to two of the three surfaces it lives on.

### AE5 — the retained requirements ledger instructs an item-by-item disposition that no artifact performs

**Artifacts.** `docs/future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md` lines
9-10 and §2; `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` §"Sources inventoried" and
§"Ledger completion check".

The requirements ledger is item 4 of the required review scope and states of itself: "This ledger
remains the retained Channel 0.1 evidence source and **must be dispositioned item by item in the
successor's migration ledger**; it is not presumed to define the 0.2 structure."

No `CH-R` or `CH-K` identifier appears anywhere in the Channel 0.2 design package — verified by
searching all ten artifacts. The migration ledger's "Sources inventoried" list names the Draft
Channel Contract, the 0.1 vectors, and three Portable Binding schemas, and does not name the
requirements ledger; its completion check enumerates Shapes, fields, message kinds, states,
categories, domains, limits, features, observation fields, the 24 vectors, the goldens, and the
consumers, and does not claim the requirements register.

Most of CH-R1 through CH-R11 is covered in substance by the field-level tables. The one that is not
merely bookkeeping is **CH-R10**, which reads "Non-promises: no delivery, **ordering**, or retry
guarantee" with disposition `decided-in-note`. That is the register entry whose answer the 2026-08-13
S1 ruling changed, and it is the requirement every finding since S1 turns on. The migration ledger
does disposition the equivalent *feature* row ("ordering guarantee unsupported" → **replaced**), but
that row is sourced from the Draft Channel Contract's feature list, so the register still carries an
undisposed, unreferenced statement of the non-promise that 0.2 core narrowed.

This is Z4's class — a requirement missing from an inventory Batch 2 works from — one artifact
further out. Nonblocking because the substance is carried elsewhere and no reader is misled about the
0.2 answer; recorded because the ledger's instruction is explicit, the coverage claim is checkable
and does not include it, and the one entry that matters is the one the whole sequence is about.

### AD2 — recorded verdict on the open owner call

The AD pass left AD2 open and asked a fresh reviewer to decide whether
`foreach ($findingFamily in @('W1', 'W6'))` in `build/verify-channel-0.2-design.ps1` is a defect or a
deliberate narrowing the comment describes badly.

**Verdict: it is a defect, and nonblocking.** The comment immediately above it states that the check
"is written over the general class — every finding a retained iteration review raises must appear in
the disposition history, **and** a finding family the review policy attributes to an iteration pass
must have a retained record — rather than over the six ids, so the next pass that skips its record
fails here too." The second half is written over two literals and evaluates two of the thirty-six
finding ids the policy bolds; it cannot fail for AA, AB, AC, AD, or anything after them. A comment
that asserts a class check over code that is a membership test of two constants is a stronger defect
than the narrow scope itself, because it is what a later reader will trust instead of reading the
loop — which is AD1's mechanism exactly. AGENTS.md's guard rule is the governing standard: "a guard
that fails when the next member is added and left out beats three assertions naming today's three."

Nonblocking, confirming the AD pass's own assessment: there is no live gap, every family does have a
retained record, and this reviewer verified that independently by deriving each retained review's
families from its `###` headings and checking each against the policy.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | Fixed/negotiated equivalence is one canonical record with byte/semantic equality; unknown versions, features, classes, and authority modes refuse; no downgrade, no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors. The brief's established-profile image carries the realization's per-interaction frame order declaration and refuses establishment when absent, and W2's point — establishment verifies the declaration is present, never true — is stated where the mutation provider is defined. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain freezing the admitted set, D1's duplicate drain fatal with the first snapshot preserved. `C2-P1` covers acceptance, rejection, and terminal monotonicity. The session totality rule leaves no event/state pair to an implementation default and does not override the nonfatal peer-interaction-during-drain row. |
| C3 | conforms | Class, direction, and external phase are separate exact admission inputs; `false` and `unknown` are treated identically; D3's receiver-local refusal is frameless `refused-local` and T1's rule that a phase refusal is never `state-violation` is carried in the ledger. Channel evaluates the predicate without advancing the phase. |
| C4 | **does not conform** | **AE1.** Everything else in C4 is sound and was verified: `C4-P1`'s three clauses, the bounded `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, the W4 retention rule and the X5 recording/retaining distinction that makes it and the property true at once, and AC3's subject clause, which this reviewer confirmed is load-bearing — reading the subject as the recording endpoint makes both conjuncts quantify over an endpoint pair no vector produces. `C4-P2`'s second conjunct is sound: probe P5 found it witnesses every reordering of a recipient's own frames and stays green on both cases the design names. The first conjunct is the finding. |
| C5 | conforms | Positional payload/authority classification, pre-dispatch parsing and bounds, no partial interaction, `known-none` on structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits must be exposed and accepted at establishment. |
| C6 | conforms | Authority is evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility are each explicitly disclaimed as grants; a local denial emits no frame and records `known-none`; cross-trust carries attributable context and no Capability. `C6-P1` requires exactly one `permitted` decision to reach dispatch. |
| C7 | conforms | Matches Decision 13's recorded Option B exactly: the CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the `interconnected && !ready` window; the composition root initiating on the Component's behalf; no Component-to-Component binding; failure preventing Ready and Release and returning the actual observation to CM4. `C7-P1` forbids the interaction producing Ready or Release by itself. |
| C8 | conforms | One terminal history; cancellation acknowledgement explicitly nonterminal; R1's held control bounded at one; R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating` with the latch not firing and the interaction outside the drain snapshot; T3's `cancelled`-with-no-request-in-force routed to `internal-channel-failure` at the recipient and `peer-fault` at the initiator, covering the class rather than the reported instance. |
| C9 | conforms | Four provenance forms with an exclusivity property; unknown peer-fault category faults locally as `unrecognized-peer-fault` with no answering fault; loss categories observer-relative. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement. PB8's finding — both stacks fabricating a known zero where the endpoint lacked evidence — is answered in C10's certainty form rather than restated here. |
| C10 | conforms-with-nonblocking-findings | **AE2.** The Y1/Y2/AC2 corrections do land: the latch and its settling frame are enumerated, the `not-applicable` value is owned, the recognized-frame-that-opens-no-interaction observation exists with its detailed reason and refused frame kind, and `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. The effect-certainty field of the one route C10 was extended to cover is unstated. |
| C11 | conforms | Facets may add classes, payload forms, and stronger evidence and may not reinterpret identities, authority, the four provenance forms, or certainty; retry is a new identity with causal attribution; the intra-interaction ordering fact is named as the one ordering fact core owns and a facet may strengthen but not weaken. `C11-P1` binds both halves. |
| C12 | conforms-with-nonblocking-findings | **AE3.** Data-only neutral contract, distinct identity spaces per realization, explicit inputs and no ambient clock or unordered enumeration, and `C12-P1`'s three clauses hold. The falsifiability requirement is stated and enforced; its converse is absent, and that absence is what let AE1 stand. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Twelve legal rows, a refused/illegal table, a totality rule that does not override named nonfatal rows, and S1-S6 as capability-wide properties. No external phase appears as a session state and each is listed as explicitly not one. Fixed/negotiated equivalence is stated as a contract defect if a field is absent from the fixed path rather than as realization freedom. |
| Interaction state | conforms | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; I1-I7 hold. The `unseen` row exists as a detailed row (X3) and routes to `unseen` rather than to the terminal state (Y3), so the `any terminal` rows do not reclaim it. AE2 is against the grid's assertion set rather than against this machine's routing. |
| State/event totality | conforms | Independently enumerated (probe P2): 108 published-row cells across three grids and 180 underlying state/event pairs. No cell is empty and no cell offers a choice between two routes. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event, and the `not-applicable` latch value is compared as a value rather than left absent. |
| Responsibility | conforms | Every row names exactly one owner identifier from a closed vocabulary of twenty-two (U2), consumers and carriers are separate columns, and the two facts the S1/AB2 line added — intra-interaction frame order and local observation content and provenance — each have a row whose crossing artifact names what AC1 made comparable, including the settling frame's arrival ordinal and the refused frame's kind. No duplicate or missing owner was found by enumeration (probe P6). |
| Completeness | conforms-with-nonblocking-findings | The disposition history runs to the ninth pass, the residual risks are stated as challenges rather than resolutions, and the U7 direction-scope row records the session-wide-versus-per-direction disagreement honestly rather than calling it undeclared. **AE3** is against the per-capability property audit's column set. The audit's own claim — that its silence is "why an unfalsifiable property survived the correction that introduced it" — is exactly right and is the reason AE3 is worth recording rather than dismissing. |
| Migration coverage | conforms-with-nonblocking-findings | All 24 predecessor vectors, twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. Z4's new-evidence inventory carries intra-interaction frame order, its two mutations, and the observation fields they compare. AC2's `unopened-interaction-identity` is in the closed detailed-reason set. **AE5** is against the requirements-register instruction the ledger does not satisfy. |
| Neutral brief | conforms | Artifact boundaries, identity spaces, the three-version rule, the vector format with W5's committing endpoint, the closed operator set with W1's precedence relation and Z1's identification-only restriction on the arrival ordinal, the parity profile carrying V1's detailed reason and X1's settling frame, the golden policy, the reordering-injection provider boundary, and the Batch 2 entry gate. The brief is correctly subordinate to the contract, both machines, and the grid, and AC1 resolved the one place it had got ahead of them. Its required adversarial group is where AE1's vector comes from — the brief names the vector correctly; the property cannot answer it. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; grid cancellation columns. The U7 direction-scope disagreement is recorded rather than hidden and does not contradict the ruling. |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan; ledger `ready` → **moved** in three places (state, message kind, feature). No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B and its explicit rejection of C and D. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's ordering sentence — a facet may strengthen the intra-interaction fact but not weaken it — is the one place the S1 ruling touches this ruling, and the two are consistent. |

The R1 (2026-08-13) and S1 (2026-08-13) correction rulings are correctly recorded as correction
rulings that do not join the fixed set of four, in both the plan and the review policy. The plan's
S1 ruling text retains `channel-core` as issued with a bracketed note that U2 normalised it to
`channel`; that is the right handling — the ruling is retained as issued and the live vocabulary is
closed elsewhere — and the identifier appears in no matrix row and in no status entry point.

## Retained findings

Every retained finding was verified in the artifact it was raised against rather than taken from the
disposition history or from an index. Spot checks were mechanised where a phrase is the whole of the
correction (probe P4). Summary:

- **B1-B4, N1-N3, F1-F3, D1-D5** — closed. Recipient `refused-local` frameless path; nonterminal
  cancellation-denial transition; one owner identifier per row; five-value disposition vocabulary;
  exact Ready ownership; peer fault from `cancel-pending`; `retained` disposition with treatment
  column; `replay-detected` live window; distinct recipient `peer-fault`/`lost`; `replaced`
  cancellation Outcome; duplicate drain fatal; distinct acknowledgement states; receiver-local phase
  to `refused-local`; the three-value latch; delivery-fallback moved to its facet.
- **T1-T4** — closed. Phase refusal is never `state-violation` (ledger); `replay-detected` bound to
  the nonterminal window; `cancelled` with no request in force routed as a class; every status block
  carries the one stable phrase and no superseded cycle name appears in any of the ten artifacts.
- **R1-R3** — closed. Held control, local unsynchronised preconditions, separate `unseen` and
  `validating` grid rows.
- **S1-S3** — S1 closed as to ownership; S3's index staleness closed and then partly recurring, which
  is AE4.
- **U1** — **closed as to falsifiability, not closed as to soundness.** See AE1. U2-U8 closed:
  vocabulary closed and the ordering row owned by `channel`; the brief carries the establishment
  declaration and the adversarial group; the disposition history runs past the eighth cycle; the
  property audit registers `C4-P2` and both mutations; the pin clause is true at this pin and now
  checked against the repository; the direction-scope row records the disagreement; the initiator
  pre-dispatch loss cell names `lost`.
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared; precedence relation added and restricted; reordering provider's declaration stated; second
  mutation added and placed in a required group; retention rule stated in three artifacts; operand
  supplied by the committing-endpoint field; latch compared; settling frame recorded and compared;
  `not-applicable` value; `unseen` transition row; recording-versus-retaining distinction; pin clause
  checked structurally; iteration reviews retained; C10 and the schema carrying the latch and settling
  frame; the observation for a recognized frame that opens no interaction; the refusal leaving state at
  `unseen`; the arrival ordinal; the ordinal restricted to identification; the grid naming a
  provenance as a provenance; C10 owning the `not-applicable` value; the ordering requirement in the
  new-evidence inventory.
- **AA1-AA3, AB1-AB2** — closed. Both indexes carry every disposition family and the counts are
  computed from the reviews directory; `channel-core` appears in no status entry point; the redesign
  plan's status block runs to AD; the matrix owns local observation content and provenance. AE4 is a
  fresh instance in the AA1 artifact, not a reopening of AA1 as framed.
- **AC1-AC4** — closed. The arrival ordinal is in all four owning artifacts; the closed detailed-reason
  set carries `unopened-interaction-identity` and C10 requires the refused frame's kind; `C4-P2`'s
  subject is named as the committing endpoint; the class check matches two-letter families.
- **AD1, AD3** — closed. The AC residual is corrected in place rather than deleted; each retained
  review's scope line and roster entry name every family it records. **AD2** remains open with a
  recorded verdict above.

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 854 local links across 301 documents |
| `build/verify-text.ps1` | pass — 880 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised
for this review. **Green gates are not evidence of conformance**, and this review found that again:
AE1, AE2, AE4, and AE5 all sit behind a fully green design gate, as every blocking finding in this
programme has.

### P2 — independent enumeration of the state/event grid

Enumerated from the state tables rather than from the grid's own count, then compared.

- Session: 6 states × 6 event families = **36** cells, all populated.
- Initiator: 6 published state groups × 6 columns = **36** published cells; expanding the groups
  (`candidate`/`admitting` = 2, `dispatched`, `cancel-pending`, `cancel-accepted`, `cancel-refused`,
  and 6 terminal states) gives 12 states × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells; expanding (`unseen`,
  `validating`, `executing`, `cancel-requested`, `cancel-refused`, and 7 terminal states) gives
  12 × 6 = **72** underlying pairs.

**108 published-row cells, 180 underlying state/event pairs.** This agrees with the eighth review's
independent count. No cell is empty, no cell offers two routes, and the closed-world rule ordering is
well-founded. AE2 is a missing *assertion within* a populated cell, not a missing cell.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`)

The policy requires at least one genuine attempt to falsify a capability-wide property, and the
handoff asks that it be done by evaluator rather than by reading. An evaluator was written from the
published prose of C4-P2 (including AC3's subject clause), the brief's closed operator set, the
brief's vector format, and the parity profile's compared fields. It imports no repository code. The
precedence operator is implemented exactly as declared — two positions in one vector's declared
ordered stimulus sequence, one endpoint, one interaction identity — and the arrival ordinal is used
only for equality.

Nine vectors from the brief's required adversarial group were run:

| Vector | Design expects | Evaluator |
| --- | --- | --- |
| conforming commit-order delivery, initiator direction | green | green |
| conforming commit-order delivery, recipient direction | green | green |
| **loss of either frame — request lost, initiator direction** | **green** | **red** |
| loss of either frame — acknowledgement lost, recipient direction | green | green |
| cancellation control for an identity the peer never opened | green | green |
| legal late control after a peer's terminal | green | green |
| duplicate terminal from a nonconformant peer | green | green |
| `C4-control-precedes-request` | red | red |
| `C4-outcome-precedes-ack` | red | red |

Both named mutations go red on the conjunct they were written for, which is the U1 correction
working. The duplicate terminal separates from a reordering only because Y4's arrival ordinal is
present — removing it collapses the two, confirming Y4 was necessary. One mismatch, which is AE1.

A follow-up structural comparison confirmed the mismatch is an indistinguishability rather than a
modelling choice: the loss vector and `C4-control-precedes-request` have equal declared stimulus
steps and equal recorded observations, so no evaluation of the published conjuncts can return
different verdicts for them.

### P4 — direct verification of retained findings

Each retained finding was checked in its own artifact. Where a correction is a phrase, the check was
mechanised across all ten artifacts with whitespace flowed, which caught two false negatives from
line wrapping before they became findings: the T4 stable phrase is present in all ten status blocks
and no superseded cycle name survives anywhere; `channel-core` appears in the responsibility matrix
only in the status block that abolishes it and in zero owner-value cells; AC1's arrival ordinal is in
all four owning artifacts; AC2's detailed reason is in all five artifacts that must carry it. Each
retained iteration review's families were derived from its `###` headings and checked against the
policy roster, which is what supports the AD2 verdict.

### P5 — exhaustive reordering coverage of `C4-P2`

The handoff asks whether the refusal-based formulation admits a reordering producing neither named
fault. Enumerated every reordering of one endpoint's own frames for one identity, given the frame
sets the contract permits — an initiator commits at most a request and one cancellation control; a
recipient commits an acknowledgement, a semantic Outcome, and a late-traffic peer fault — and ran each
through the evaluator:

| Reordering | Witnessed |
| --- | --- |
| initiator: control before request | red — conjunct 1 |
| recipient: Outcome before acknowledgement | red — conjunct 2 |
| recipient: peer fault before acknowledgement | red — conjunct 2 |
| recipient: peer fault before Outcome | red — conjunct 2 |

**No uncovered reordering exists.** The answer to that question is that the formulation is complete
in the direction it was audited for, and the last two are caught although neither is a named
mutation. The same is true of the second handoff question: restricting both conjuncts to one
endpoint's own frames leaves no forbidden reordering unwitnessed, because the promise is itself
per-endpoint. AE1 is in the orthogonal direction nobody probed.

### P6 — ownership and upstream consistency

- Every `Semantic owner` cell in the matrix draws from the declared closed vocabulary; enumeration
  found no identifier used before declaration and no row with two owners or none.
- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with no ratified architecture. Recomputed SHA-256
  for the architecture document, the 0.8 requirements, and both stack plans: **all four match the
  registry**.
- The architecture document's own header carries the same Complete Draft status, and both stacks'
  READMEs state `Designed for: Brontide Architecture 0.8, Complete Draft, not ratified`. The design
  package's `Designed for` line agrees, and no artifact treats 0.8 as ratified or claims Channel 0.2
  conformance.
- Decision 13's recorded ruling (Option B for 0.2; C and D rejected) matches C3, C7, and the plan's
  relational-initialization ruling clause for clause, including the composition root standing in for
  the initiating Component and the refusal to introduce a Component-to-Component binding kind.
- PB8's blocking finding in both stacks — process loss still fabricating a known zero effect count —
  is answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect, which
  is the correct disposition.

### P7 — clone completeness and pin

886 tracked paths, 886 files on disk, empty `git diff HEAD`, detached HEAD at
`940894839e2abe9a3e54536b2ed24c1f18bf6598` whose subject is the commit the policy names. No design
artifact was read from outside the clone.

## What this verdict means

The U1 correction is real work and most of it holds. `C4-P2` can now fail, both conjuncts have a
mutation, the mutation can be executed, the property is writable in the declared language, its
witnesses are recorded and compared, and its subject names the right endpoint. Probe P5 says the
formulation is complete against every reordering the promise forbids. That is nine families of
correction doing what they were for.

What it does not yet have is the other half of a decision procedure. Nine passes asked *can this
property fail*; none asked *can this property fail when nothing is wrong*, because no artifact
requires an answer to the second question — that is AE3, and AE1 is the instance it let through. The
loss vector is not an exotic construction: it is one of the seven members of the required adversarial
group the brief itself wrote, and it is produced by the ordinary interaction of two things the
contract independently permits, C4's allowance that loss may drop a frame and C8's allowance of one
cancellation control from `dispatched`.

The pattern the handoff describes still holds, with one correction to it. The findings have been in
the layer under the previous fix, and the AC and AD families showed that they are also in the layers
several families back and in the records about the design. AE1 is a third direction: the layer under
the *question*. Every pass has evaluated these properties against the inputs the design nominates as
interesting, and the design nominates them from the same narrative that produced the property. The
loss vector was nominated by the brief and never evaluated, including by the evaluator the preceding
pass ran and reported as finding nothing. A reviewer that inherits this attestation should assume the
same is true of some other property here — `C10-P1`, `C4-P1`, and `I5` all quantify over vector sets
nobody has enumerated — and should evaluate each against the vectors the *brief* requires rather than
the vectors the capability's prose discusses.

Batch 2 remains closed. No schema, public type, package, host, provider, or encoding is authorized.
`channel-0.2-design-foundation-closure-record.md` is not created by this review and must not be,
because this verdict does not conform. The design was not repaired here: this attestation is the only
file this reviewer wrote, and nothing in the clone was committed.

## Note on the design gate

The gate results in P1 are from before this attestation existed. Retaining it makes
`build/verify-channel-0.2-design.ps1` fail with exactly three failures, all of them the same fact:

```text
FAIL: The Channel 0.2 design foundation must retain exactly the review README, all eight negative
      attestations, and all four correction iteration reviews before the next closure review.
FAIL: The Channel index's Design reviews row does not say '9 negative attestations' ...
FAIL: The future-work index does not say '9 retained independent reviews' ...
```

This is the verifier working as designed, not a defect this review introduced. The expected-file set
and the two computed counts are pinned to the pre-review state, and step 5 of the policy's exact-next-work
section is where the correction pass updates the verifier, the Channel index, and the future-work
index together. `build/verify-doc-links.ps1` (855 links across 302 documents) and
`build/verify-text.ps1` (881 files) both pass with the attestation present.

The AA1/AA2 counting checks are worth a word of credit here: they fired immediately and named the
exact strings that must change. That is the behaviour AGENTS.md asks of a guard — failing when the
next member is added and left out — and it is the contrast that makes the AD2 verdict above clear.
