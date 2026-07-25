# Implementation correction status

Status: completed permanent implementation record for the correction programme. The controlling
temporary plan was deleted after the strict gate, two conforming independent reviews, and explicit
checked authorization. The [completion report](./implementation-correction-completion-report.md)
explains the delivered work; the [independent-review records](../../../conformance/reviews/README.md)
preserve the machine-checkable closure decision. This record reports evidence and does not replace
architecture.

| Finding | Local implementation status | Permanent evidence |
| --- | --- | --- |
| A — Minimal Base authority | Implemented and tested | Minimal Architecture 0.5 matrix; `Brontide.Minimal.Conformance`; `Minimal/CHANGELOG.md` migration record |
| B — stable traceability | Implemented and mechanically checked | `conformance/requirements.json`; both stack matrices; `build/verify-evidence.ps1` |
| C — interchange breadth and cost | Implemented and tested in both process directions | Cooling and Catalog fixtures/matrices; adversarial vectors; `interchange/binding-measurements.json`; cross-process suites |
| D — engineering controls | Implemented and validated locally and in both CI SDK lanes | two-band CI workflow; SDK/versioning/stewardship policy; text/link/project/assembly verification scripts |
| E — maintainability and performance | Implemented for the corrected surface | module/API review maps; separate Catalog transport modules; two owned benchmark executables; public boundary threat/operability contract |
| F — independent-review invariant gaps | Implemented, fully gated, and independently reviewed conforming at `2049554c8e7ee5c26e4fcae6a103997737aa90f2` | Reference Genesis/escaped-authority/provenance/liveness/concurrency tests and matrix; Minimal Fragment-host-lineage and failed/discarded-Genesis identity tests and matrix; both changelogs; final attestations |

The prior full gate passed locally from a clean worktree and in both GitHub Actions SDK lanes at
commit `69628a194834454169014b5b05dc8a6c2ad4d812` (run `29656449122`). Independent review then
reproduced Finding F. A first correction passed the complete local two-stack gate on 2026-07-23;
the fresh review then found that callback-held authority references could outlive rollback. That
deeper Reference transaction boundary was corrected; a later fresh review then found that Minimal
recycled an escaped Actor identity after a failed persistent-World Genesis branch. The next review
then found same-transaction persistent-branch collisions in Minimal and incomplete restoration of
pre-existing mutable lease state in Reference. The following Minimal review found that a retained
pre-transaction World alias was outside the transaction guard. The next Reference review found
that a newly issued lease removed from the registry could still renew through its escaped object.
The subsequent Reference review found that concurrent context issuance could pass its activity
check and resume after rollback. The activity check and mutation were serialized under the domain
gate, a permanent three-issuer regression was added, and the complete clean-worktree gate passed at
the final target. Fresh Reference and Minimal reviews then found all 16 retained requirements
conforming for each stack. Their canonical attestation hashes are recorded in the closure and
completion report.

The machine-checkable [independent-review framework](../../../conformance/reviews/README.md) pins that
commit, both evidence matrices, the stable requirement vocabulary, and the finding-closing commits.
It generates separate per-stack packets and enforces completeness, traceability, review-result
consistency, and the final deletion authorization. It cannot supply the independent semantic
judgment; that judgment is preserved in the two completed conforming attestations. The authorized
closure record pins their exact canonical hashes and every finding-closing commit.

The pinned policy permits those attestations to be produced by an automated reviewer operating
under a distinct identity in a fresh isolated context without access to the implementation
session's private reasoning. The review request pins the central architecture status registry and
requires an explicit assessment of the current architecture and each stack's locally stated target.
Automated review remains valid unless the registry or an explicit repository policy changes the
rule.
