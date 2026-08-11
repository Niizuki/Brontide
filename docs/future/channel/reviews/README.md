# Channel 0.2 design-foundation reviews

Status: four owner rulings resolved; five negative independent reviews retained; B1-B4, N1-N3,
F1-F3, D1-D5, and T1-T4 have correction passes and a fresh independent closure re-review is pending.

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

The next agent closes the first batch; it does **not** begin schemas or implementation.

1. Review the immutable T1-T4 correction pin
   `11ba93bddbd38f03df59b4afc5166d7c6991c865` from a fresh isolated clone and a reviewer
   identity distinct from the correction author and all five retained reviewers. Read `AGENTS.md`,
   this policy, all five retained attestations, and every artifact in the required scope. Enumerate
   the complete session/initiator/recipient state-event grid independently, attempt to falsify at
   least one Cn-P1 property, explicitly decide B1-B4, N1-N3, F1-F3, D1-D5, and T1-T4, and search for
   new blockers.
2. The reviewer writes only
   `channel-0.2-design-foundation-closure-re-review-attestation.md`. It records the full pin,
   isolation, per-capability/area/ruling verdicts, retained-finding decisions, probes, and either
   `conforms` or exact new findings. It does not repair the design or create a closure record.
3. If the verdict is negative, retain the attestation unchanged, add failing verifier/model checks
   for each new finding, correct the design in a later commit, and obtain another fresh review. Batch
   2 remains closed.
4. If the verdict conforms, retain and commit the attestation unchanged, calculate its SHA-256, then
   create `channel-0.2-design-foundation-closure-record.md`. The record contains the reviewed commit,
   attestation path and hash, reviewer identity/date/verdict, all four owner rulings, confirmation
   that every retained finding closed with no new blocker, and the exact validation results. Update
   this README, the Channel index, the redesign plan, `docs/future/README.md`, and the design verifier
   so they accept exactly the conforming attestation and closure record and say Batch 2 is open.
5. Run, in order:

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

The T1-T4 correction pin is `11ba93bddbd38f03df59b4afc5166d7c6991c865`
(`docs(channel): widen the invalid cancelled terminal to its class`, committed
2026-08-11T20:42:21+02:00). Review that commit or any later commit whose design artifacts hash
identically to it.

No conforming closure attestation exists yet. The corrected artifacts remain nonconforming evidence
until a fresh reviewer closes every retained finding and reports no new blocker.

## Disclosed process deviation in the T1-T4 correction

The totality review and the T1-T4 correction pass were performed in one session by
`agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`, on the repository owner's
explicit instruction, rather than by separate reviewer and author actors. This departs from the rule
above that a reviewer does not repair the design it reviews, and it is recorded here so the next
reviewer weighs the T1-T4 corrections knowing their author also wrote the attestation that found
them. The retained attestation itself is unmodified, and the independence requirement on the next
cycle is unchanged: its reviewer must differ from that identity and from all five retained
reviewers.
