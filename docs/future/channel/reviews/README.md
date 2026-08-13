# Channel 0.2 design-foundation reviews

Status: four owner rulings resolved; seven negative independent reviews retained. B1-B4, N1-N3,
F1-F3, D1-D5, T1-T4, and R1-R3 are closed in the artifacts they were raised against, R1-R3 having
been re-verified individually by the seventh review rather than taken from an index. That review's
blocking **S1** — the R1 correction kept `rejected-protocol` at recipient `unseen` while the fact
making that sound, intra-interaction delivery order, was asserted in the state/event grid alone and
disclaimed by C4 and C11 and assigned elsewhere by the responsibility matrix — is corrected under the
2026-08-13 S1 ruling giving that fact an owner. Nonblocking S2 and S3 are dispositioned. A fresh
independent closure re-review is pending on the S1 correction pin. R1 and S1 each required their own
dated owner ruling, recorded in the redesign plan; both are correction rulings and neither joins the
four first-batch rulings, which remain the fixed set recorded on 2026-08-11.

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

The seventh review has run, from a fresh isolated clone, and returned `does-not-conform`; its
retained record is `channel-0.2-design-foundation-closure-review-7-attestation.md`. It closed R2 and
R3, confirmed every earlier finding still closed, and raised blocking **S1** with nonblocking **S2**
and **S3**. **Steps 1 through 3 are complete.** Step 4 is the live path. The next agent reviews the
S1 correction; it does **not** begin schemas or implementation, and it does not create
`channel-0.2-design-foundation-closure-record.md` unless its own verdict conforms.

1. ~~Obtain an owner ruling on S1.~~ **Done** on 2026-08-13: Channel 0.2 core owns intra-interaction
   frame order, narrowly scoped. Holding at `unseen` was rejected because the hold has no bound —
   admission at `validating` is local and terminates, while at `unseen` the recipient waits on a peer
   frame that may never arrive and core has no timeout, deadline, or expiry concept. Requiring a
   delivery facet of any cancelling profile was rejected as dragging persistence and retry into every
   such profile. The option set is in the
   [redesign plan's resolved questions](../Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md#resolved-questions).
2. ~~Add a failing check for **S1** before correcting it.~~ **Done.** The design verifier now asks the
   ownership question directly: it keys off the recipient grid's `unseen` cancellation verdict — the
   thing that *depends* on the ordering fact, rather than the sentence that asserts it, so deleting
   the sentence cannot make the check pass vacuously — and then requires C4 to promise the fact, C4's
   silence and C11 not to disclaim it, the responsibility matrix to own it, the realization profile
   to declare it, and C4 to carry a property and a mutation scenario for it. It failed with eight
   findings before the correction and was mutation-tested afterwards by removing the matrix row,
   reverting C4's promise, and renaming `C4-P2`, each of which fires it again.
3. ~~Correct S1 contract-first, and disposition S2 and S3.~~ **Done.** The ordering promise is stated
   in C4 with `C4-P2` and the `C4-control-precedes-request` mutation vector; C4's silence and C11 are
   scoped to cross-interaction and cross-session ordering; the matrix carries an
   `Intra-interaction frame order` row owned by `channel-core`; the realization profile declares
   per-interaction frame order; the grid now carries the fact rather than owning it; and the
   completeness review records both it and the held-control gap. S2 added `validating` loss and drain
   rows and reconciled the pre-dispatch loss rule to any nonterminal state. S3 was closed in the
   commit that recorded the review, because it was entirely index and status staleness and leaving it
   while recording a seventh review would have committed the very defect it names.
4. Obtain another fresh independent review of the corrected pin, from a reviewer identity distinct
   from the correction author and all seven retained reviewers, **in a fresh isolated clone**. Its
   scope, verdicts, and probe requirements are unchanged from the sections above. It writes only its
   own attestation.

   The reviewer should treat the S1 correction as the primary target. The lesson three cycles have
   now paid for is that **a correction resolving a contradiction by asserting a new fact must also
   say who owns it** — so the reviewer reads the contract, both machines, the grid, and the
   responsibility matrix as competing claims about the same facts, and checks ownership rather than
   only agreement. Applied to this correction, the sharpest questions are whether promising
   intra-interaction frame order has quietly created obligations elsewhere — what it means for a
   realization that batches or multiplexes, whether `C4-P2` can actually fail in a stack that cannot
   reorder, and whether the recipient-to-initiator direction needs the same promise the
   initiator-to-recipient direction now has.
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

The S1 correction pin is the commit titled
`fix(channel)!: give intra-interaction frame order an owner`, committed 2026-08-13, which is the
current review target. Review that commit or any later commit whose design artifacts hash identically
to it. The preceding pin `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4` is what the seventh review
assessed and is nonconforming.

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
so this deviation is confined to the T1-T4 pass and has not recurred.
