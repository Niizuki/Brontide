# Channel 0.2 design-foundation reviews

Status: four owner rulings resolved; eight negative independent reviews retained. B1-B4, N1-N3,
F1-F3, D1-D5, T1-T4, R1-R3, and S1-S3 are closed in the artifacts they were raised against, the
eighth review having re-verified every one of them individually rather than taking closure from an
index. That review's blocking **U1** — the S1 correction gave intra-interaction frame order an owner
but attached `C4-P2` to it, and `C4-P2` quantified over the frames a recipient *accepts* while the
design refuses every reordered frame, so the accepted sequence was empty and the property stayed
green on its own named mutation — is corrected by restating `C4-P2` over the refusal a reordering
produces. Nonblocking **U2**-**U8** are now also corrected, as are **V1** and **V2**, which the
[U1 correction iteration review](./channel-0.2-u1-correction-iteration-review.md) raised against the
U1 correction itself. Every finding this programme has recorded is closed in the artifacts it was
raised against, and none of that is a verdict: a fresh independent closure re-review is pending on
the U2-U8 correction pin, and it is the only thing that can close the batch. R1 and S1 each required their own
dated owner ruling, recorded in the redesign plan; both are correction rulings and neither joins the
four first-batch rulings, which remain the fixed set recorded on 2026-08-11. The U1 correction needed
no ruling: it was a property that could not fail, which is a defect rather than a choice.

The cycle is deliberately unnamed. Reviews are numbered, not titled, and every artifact says it awaits
"a fresh independent closure re-review" whichever cycle is current — that phrase is the T4 correction
and must survive each round rather than being escalated.

Review cycles are numbered from here rather than named. The first four were called "closure",
"final closure", "definitive closure", and "totality closure", and that escalation is what produced
T4: three artifacts were left naming a cycle that had already run. Every artifact now says it awaits
"a fresh independent closure re-review", and the design verifier rejects a status block that names a
superseded cycle.

This directory retains independent attestations for the complete Channel 0.2 first-batch design
foundation. A review is independent only when its reviewer identity differs from the design author,
it runs in a fresh isolated context at one pinned commit, and it has no access to the author's private
reasoning.

## Two kinds of review

The independence requirement exists to keep a *closing* judgement free of the bias that made the
defect invisible in the first place. It was never a rule against working on the design. Conflating
the two cost this programme a cycle, so the distinction is now explicit.

**Iteration review.** An author-side pass over work in progress. It may share an actor and a context
with the correction it examines, may iterate as many times as there are findings, and may correct
what it finds in the same pass. It is retained as evidence, is named `*-iteration-review.md` rather
than `*-attestation.md`, and states its own non-final status. It **cannot** close the first batch,
cannot authorize Batch 2, cannot produce the closure record, and its verdict is never the conforming
verdict the Closure section requires — however clean it is. Its value is that it finds and fixes
defects cheaply, before a fresh reviewer spends its one shot of cold context on them.

**Independent closure review.** The judgement that can close. It runs in a fresh isolated clone at one
pinned commit, under a reviewer identity distinct from every retained reviewer and from every
correction author, with no access to author private reasoning and no history of having worked on the
artifacts. Only this kind produces an attestation, and only a conforming one opens Batch 2.

Iterating in one context is therefore encouraged for as long as findings remain. What may not happen
is an actor declaring its own work finished. Marking the batch closed is reserved to the independent
closure review, and an iteration review that reports no findings means the work is ready *to be
reviewed*, not that it passed.

## Required review scope

The reviewer reads and assesses:

1. `Brontide-Architecture-Status.json` and the current Architecture 0.8 document, including its
   Complete Draft/non-ratified status;
2. both stacks' local Architecture 0.8 targets and public-boundary limitations;
3. the Channel 0.2 redesign plan;
4. retained Channel 0.1 design, contract, requirements ledger, 24 vectors, Portable Binding neutral
   schemas, and PB8 closure findings;
5. Decision 13 and its exact CM3/CM4 relational lifecycle requirements;
6. C1-C12, including every named scenario, capability-wide property, evidence mode, and explicit
   silence;
7. both state machines and all legal/illegal/terminal paths;
8. the closed state/event coverage grid, including every catch-all and late-traffic latch;
9. every responsibility-matrix owner and neutral crossing artifact;
10. the completeness review, including its residual risks;
11. every migration-ledger inventory and disposition; and
12. the neutral contract/vector brief and Batch 2 entry gate.

## Required verdicts

An attestation records:

- reviewer identity, reviewed commit, date, and isolation method;
- overall `conforms`, `conforms-with-nonblocking-findings`, or `does-not-conform`;
- one verdict and rationale for each C1-C12;
- session-state, interaction-state, state/event-totality, responsibility, completeness,
  migration-coverage, and neutral-brief verdicts;
- confirmation that each of the four resolved owner rulings is represented consistently throughout
  the first-batch design;
- every blocking and nonblocking finding with exact artifact/section evidence; and
- checks/probes performed, including at least one attempt to falsify a capability-wide property.

The reviewer writes only its requested attestation. It does not repair the design it reviews. A
blocking finding is corrected test/contract-first in a later commit and receives a fresh re-review;
the original negative attestation remains retained.

## Closure

Batch 2 may begin only after:

- architecture owners record the four first-batch rulings;
- every blocking review finding is corrected;
- a fresh closure attestation conforms at the corrected commit; and
- a small closure record pins the reviewed commit and attestation hash.

The author correction pass and ordinary documentation gates are not independent review.

## Exact next work

The eighth review has run, from a fresh isolated clone, and returned `does-not-conform`; its retained
record is `channel-0.2-design-foundation-closure-review-8-attestation.md`, and the seventh review's
remains `channel-0.2-design-foundation-closure-review-7-attestation.md`. It verified every retained
finding through S1-S3 closed individually, found S1 closed as to ownership but not as to
falsifiability, and raised blocking **U1** with nonblocking **U2**-**U8**. **Steps 1 through 3 are
complete.** Step 4 is the live path. The next agent reviews the U1 correction; it does **not** begin
schemas or implementation, and it does not create
`channel-0.2-design-foundation-closure-record.md` unless its own verdict conforms.

1. ~~Obtain an owner ruling on U1.~~ **Not required, and this is the reason.** S1 and R1 were choices
   between defensible designs and each needed a ruling. U1 was not: `C4-P2` asserted that
   `C4-control-precedes-request` was the mutation it must go red on, and it stayed green on it. A
   property that cannot fail is a defect against C12's own rule that "every property must be able to
   fail against a named incorrect implementation", so the correction restores what the design already
   claimed rather than selecting between options.
2. ~~Add a failing check for **U1** before correcting it.~~ **Done.** The design verifier keys off the
   claim that *depends* on falsifiability — C4 asserting that `C4-control-precedes-request` is the
   mutation `C4-P2` must go red on — rather than off the property's own wording, so deleting that
   claim cannot make the check pass while leaving an untestable promise standing. It then requires
   `C4-P2` to be stated over the refusal a reordering produces, to carry both direction conjuncts
   restricted to one endpoint's own frames, not to give the mutation a contradictory "rejected as
   nonconforming evidence" expectation, and the per-capability property audit to register the pair. It
   failed with five findings before the correction and was mutation-tested afterwards by weakening
   each conjunct, restoring the contradictory sentence, reverting the audit row, and renaming
   `C4-P2` — each of which fires it again.
3a. ~~Correct the nonblocking findings U2, U3, U4, U7, and U8.~~ **Done**, each with a failing check
   written first and mutation-tested after. The responsibility matrix now declares a closed
   owner-identifier vocabulary and the ordering row is owned by `channel`, not a second name for the
   same family (U2). The neutral brief's establishment rule carries the realization's per-interaction
   frame order declaration and the required adversarial groups include one owning the ordering
   mutation (U3). The completeness review's disposition history runs to the eighth cycle instead of
   stopping at the fifth (U4), and its in-flight direction-scope row records that `C4-P1` and `I5`
   read session-wide while the reservation mechanism can enforce only per-direction, rather than
   calling the scope undeclared (U7). The initiator grid's pre-dispatch Local loss cell names `lost`
   like every other cell in that column (U8). One of these checks was itself found weak by mutation
   testing — a phrase-anywhere test that the artifact's own status block satisfied — and was scoped to
   the section that has to carry the rule.
3. ~~Correct U1 contract-first.~~ **Done.** `C4-P2` is restated over the refusal reordering produces
   rather than over the accepted sequence: no endpoint records a recipient `rejected-protocol` at
   `unseen` for a cancellation control whose request the same endpoint had already committed, and none
   records a late-traffic `state-violation` latched against a frame the same endpoint committed before
   the frame that made the interaction terminal. Restricting each conjunct to one endpoint's own
   frames is load-bearing, and was found by probe rather than by reading: without it a legal late
   control after a peer's terminal, and a duplicate terminal from a nonconformant peer, both fail the
   property. The mutation vector's expected observation is now the recipient's recorded refusal, which
   is a determinate portable observation under C12-P1, rather than the vector being rejected before it
   executes. The per-capability property audit registers `C4-P2` and its mutation.
4. Obtain another fresh independent review of the corrected pin, from a reviewer identity distinct
   from the correction author and all eight retained reviewers, **in a fresh isolated clone**. Its
   scope, verdicts, and probe requirements are unchanged from the sections above. It writes only its
   own attestation.

   The reviewer should treat the U1 correction as the primary target, and should treat the disclosed
   process deviation below as a reason to weigh it harder rather than less. The lesson four cycles
   have now paid for is that **a correction is not finished when the fact has an owner; it is finished
   when a property can refute it** — so the reviewer should not read `C4-P2` and agree with it, but
   write an evaluator from the published prose and run the mutation through it, as the eighth review
   did. The sharpest questions: does the refusal-based formulation admit a reordering that produces
   neither named fault; does restricting both conjuncts to one endpoint's own frames leave any
   reordering the promise forbids unwitnessed; can the recipient's `rejected-protocol` at `unseen` be
   distinguished in the observation record from the other causes of that same terminal, given C10
   carries no frame-order field; and do **U2**-**U8**, all still open, interact with the correction.
5. If that verdict conforms, retain and commit the attestation unchanged, calculate its SHA-256, then
   create `channel-0.2-design-foundation-closure-record.md`. The record contains the reviewed commit,
   attestation path and hash, reviewer identity/date/verdict, all four owner rulings, confirmation
   that every retained finding closed with no new blocker, and the exact validation results. Update
   this README, the Channel index, the redesign plan, `docs/future/README.md`, and the design verifier
   so they accept exactly the conforming attestation and closure record and say Batch 2 is open.
6. Run, in order:

   - `build/verify-channel-0.2-design.ps1`;
   - `build/verify-channel-0.2-design.ps1 -NegativeProbe` and confirm it fails only because
     `C12-P1` was removed in memory;
   - `build/verify-doc-links.ps1`;
   - `build/verify-text.ps1`; and
   - `build/verify-interchange.ps1`.

Only after the conforming attestation, closure record, documentation/status updates, and clean full
gate are committed may the next agent start Batch 2 from the
[neutral contract brief](../Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md). This closure
authorizes planned schema work; it does not ratify Channel 0.2 or claim implementation conformance.

## Retained attestations

- [Original design-foundation review](./channel-0.2-design-foundation-attestation.md) — reviewed
  `66729b097b032febf498dd907dd2387e2aebc2c5`; `does-not-conform`; B1-B4 retained for closure
  comparison.
- [First closure review](./channel-0.2-design-foundation-closure-attestation.md) — reviewed
  `e863bf15fca30466d6e262b0ea66b3c05bc384eb`; `does-not-conform`; B1-B4 closed and N1-N3 retained
  for final closure comparison.
- [Final closure review](./channel-0.2-design-foundation-final-closure-attestation.md) — reviewed
  `1af7ba0018c874750e346ee687f07ea1d302adef`; `does-not-conform`; B1-B4/N1-N3 closed and F1-F3
  retained for definitive closure comparison.
- [Definitive closure review](./channel-0.2-design-foundation-definitive-closure-attestation.md) —
  reviewed `1b7c5fdea0dc555a64152eea055fcebad053cf90`; `does-not-conform`; all earlier findings closed and
  D1-D5 retained for totality closure comparison.
- [Totality closure review](./channel-0.2-design-foundation-totality-closure-attestation.md) —
  reviewed `5cf42c4d97083324ffb8d6bd68491a145b8e611a`; `does-not-conform`; D1-D5 closed, blocking T1
  and nonblocking T2-T4 retained for closure re-review comparison.
- [Closure re-review](./channel-0.2-design-foundation-closure-re-review-attestation.md) — reviewed
  `11ba93bddbd38f03df59b4afc5166d7c6991c865`; `does-not-conform`; T1-T4 closed, blocking R1 and
  nonblocking R2-R3 retained for the next closure comparison. **Its isolation is partial and the
  attestation says so**: no fresh isolated clone was used, and the reviewing session had already read
  the future index and this policy while identifying the work. Under the owner ruling recorded in
  [`docs/future/README.md`](../../README.md#channel-02-first-batch-remaining-work), navigational
  reading of the indexes to locate the work does not by itself spend context freshness; the absent
  isolated clone still does. It therefore establishes R1 but could not have closed the batch.
- [Closure review 7](./channel-0.2-design-foundation-closure-review-7-attestation.md) — reviewed
  `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`; `does-not-conform`; T1-T4, R2, and R3 closed, R1 closed
  at `validating` but **not** at `unseen`, blocking S1 and nonblocking S2-S3 retained for the next
  closure comparison. **Its isolation is complete**: a fresh isolated clone was used, the reviewer
  identity differs from all six earlier reviewers and from the correction author, and no author
  private reasoning was available. The attestation records what its dispatching brief did name — the
  primary target and three specific silence checks — and states that S1 was not among them and was
  reached independently.

- [Closure review 8](./channel-0.2-design-foundation-closure-review-8-attestation.md) — reviewed
  `3b27e3a85bf018bead6d226a13d075c7e6ed16fa`; `does-not-conform`; every retained finding through
  S1-S3 verified closed individually, S1 closed as to ownership but **not** as to falsifiability,
  blocking **U1** and nonblocking **U2**-**U8** retained for the next closure comparison. **Its
  isolation is complete**: a fresh isolated clone at a short path, 881 tracked paths materialised,
  reviewer identity distinct from all seven earlier reviewers and from every correction author, and
  no author private reasoning available. It reproduced the grid enumeration independently (108
  published-row cells, 180 underlying state/event pairs) and reached U1 by writing a property
  evaluator from the published prose and running the property's own named mutation through it. The
  attestation records that U1 answers a question its dispatching brief named, and that U2-U8 did not.

The current review target is the commit titled `fix(channel): close U2, U3, U4, U7, and U8`, committed
2026-08-14, which is the head of the U1/U2-U8 correction sequence beginning at
`fix(channel): make C4-P2 falsifiable`. Review that commit or any later commit whose design
artifacts hash identically to it — and check that claim rather than assuming it, because the
preceding cycle's pin clause went stale exactly that way and the eighth review raised it as **U6**.
The preceding pin `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4` is what the seventh review assessed and
is nonconforming.

No conforming closure attestation exists yet. The corrected artifacts remain nonconforming evidence
until a fresh reviewer closes every retained finding and reports no new blocker.

## Disclosed process deviation in the T1-T4 correction

The totality review and the T1-T4 correction pass were performed in one session by
`agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`, on the repository owner's
explicit instruction, rather than by separate reviewer and author actors. This departs from the rule
above that a reviewer does not repair the design it reviews, and it is recorded here so the next
reviewer weighs the T1-T4 corrections knowing their author also wrote the attestation that found
them. The retained attestation itself is unmodified, and the independence requirement on the next
cycle is unchanged: its reviewer must differ from that identity and from all seven retained
reviewers.

The sixth and seventh reviews were both performed by reviewers separate from the correction author,
so this deviation was confined to the T1-T4 pass until the U1 pass below.

## Disclosed process deviation in the U1 correction

The eighth review and the U1 correction pass were performed in one session by
`agent:claude-opus-5-channel-0.2-closure-review-8-2026-08-14-3b27e3a`, on the repository owner's
explicit instruction, rather than by separate reviewer and author actors. It is recorded here rather
than left implicit, because an undisclosed reviewer-repairs-own-finding is precisely the defect class
this programme exists to catch.

Under the two-kinds-of-review section above, the correction pass and the
[U1 correction iteration review](./channel-0.2-u1-correction-iteration-review.md) that followed it are
legitimate author-side work rather than deviations. What remains a deviation is narrower and is the
part that matters: the actor that wrote closure review 8's attestation then corrected the blocking
finding that attestation raised. The next closure reviewer weighs the U1 correction knowing its author
also wrote the attestation that found it, and knowing the author had published a proposed fix before
being asked to apply it — so the correction was not derived independently of the review that motivated
it.

The retained attestation
[`channel-0.2-design-foundation-closure-review-8-attestation.md`](./channel-0.2-design-foundation-closure-review-8-attestation.md)
is **unmodified** by this pass and still reads as it did when the verdict was returned, including its
sentence that the design was not repaired there. That sentence was true of the review commit and is
superseded by this one; the attestation is retained rather than corrected, which is the policy for
every retained attestation.

The independence requirement on the next cycle is unchanged and now stricter by one name: its
reviewer must differ from all eight retained reviewers and from this correction author, which are the
same identity.
