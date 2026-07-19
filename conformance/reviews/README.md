# Independent implementation review

This directory contains the machine-checkable control plane for independent implementation
verification. It does not turn conformance judgment into a script. The scripts freeze the review
target, generate complete evidence packets, check reviewer separation statements, prevent missing
requirement decisions, require consideration of the current architecture, and make the
temporary-plan deletion gate enforceable.

The pinned request is [`review-request.json`](./review-request.json). It pins the central
[`architecture status registry`](../../Brontide-Architecture-Status.json), which alone identifies
the current architecture, latest ratified architecture, retained implementation baseline, current
delivery plans and ledgers, requirement vocabulary, and per-stack evidence matrices. The request
also fixes the reviewed commit, correction-closing commits, and expected record paths. Historical
matrices remain valid evidence; they are not allowed to replace the registry-selected architecture
in review.

Any change to a pinned source invalidates the request instead of silently changing what the
reviewer was asked to inspect. Verification needs the pinned Git objects locally; CI therefore
checks out full history. Record hashes use canonical UTF-8 text with LF line endings so the same
content has the same digest on Windows and CI checkouts.

## What requires independent judgment

An independent reviewer must decide whether the implementation and its negative evidence actually
satisfy each requirement. A green build proves reproducibility, not architectural correctness. The
reviewer must not be an implementation actor named in the request and must disclose conflicts. One
reviewer may examine both stacks, but the stacks require separate packets and conclusions. Two
reviewers are preferable when available.

The reviewer may be human or automated. An automated attestation has the same closure weight as a
human attestation when the reviewer has a distinct identity, starts in a fresh isolated context,
has no access to the implementation session's private reasoning, and completes the same review
record. This rule follows the current architecture rather than a special version and remains valid
until the current architecture or an explicit repository policy changes it.

For every retained implementation requirement, the reviewer inspects the source and both positive
and negative evidence, runs or extends tests where needed, and records one of:

- `conforms`;
- `approved-disposition`, with architecture-approved evidence;
- `does-not-conform`, which preserves the finding and blocks closure.

The reviewer separately reads the complete current architecture, the stack's current implementation
plan, and its delivery ledger. It records whether the implementation is consistent with current
direction, has accurately recorded current deltas, or contains a blocking conflict. A
`current-deltas-recorded` assessment is honest for planned but unimplemented current work and does
not turn the final correction verdict into a current-architecture conformance claim.

The framework checks that every applicable stable ID has exactly one reviewed decision. It cannot
prove that a reviewer was truthful or that their reasoning was competent; Git review controls or an
external signed review can strengthen identity assurance.

## Review workflow

Use a clean checkout for the pinned target and a separate checkout containing this framework. A
fresh automated review task may generate packets like this:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\new-independent-review.ps1 `
  -Stack Reference -ReviewerId "agent:codex-independent-review" `
  -ReviewerName "Codex independent reviewer" -ReviewerKind Automated `
  -AutomationSystem "Codex" -AutomationSessionId "task:<fresh-task-id>"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\new-independent-review.ps1 `
  -Stack Minimal -ReviewerId "agent:codex-independent-review" `
  -ReviewerName "Codex independent reviewer" -ReviewerKind Automated `
  -AutomationSystem "Codex" -AutomationSessionId "task:<fresh-task-id>"
```

For a human reviewer, omit `-ReviewerKind`, `-AutomationSystem`, and `-AutomationSessionId`.

In the clean target checkout, detach at the `reviewTargetCommit` from `review-request.json`, run the
complete gate, and inspect the evidence listed in each packet:

```powershell
git switch --detach 69628a194834454169014b5b05dc8a6c2ad4d812
.\build\verify-interchange.ps1
```

Complete the generated JSON records without changing the generated requirement or evidence
snapshots. Set the gate result, review flags, rationale, verdict, overall verdict, timestamps, and
attestation fields. An automated reviewer also sets `freshContext` to `true` and
`implementationContextAccess` to `none`. Complete `currentArchitectureReview` using
`current-direction-consistent`, `current-deltas-recorded`, or `blocking-conflict`. Then return to
the framework checkout and run:

```powershell
.\build\verify-independent-review.ps1
```

The default check validates records that exist and reports absent records as pending, so the normal
repository gate stays useful while an external review is in progress. A malformed or internally
inconsistent record always fails.

After both reviews conform, generate the closing record:

```powershell
.\build\new-independent-review-closure.ps1
```

A maintainer checks its pinned review hashes and finding commits, fills the authorization fields,
and verifies the deletion gate:

```powershell
.\build\verify-independent-review.ps1 -RequireComplete
```

Only after that command passes may the temporary correction plan be deleted. Once the plan is
absent, the verifier automatically enables `-RequireComplete`; deletion without the two reviewed
attestations and authorized closure therefore fails the full repository gate.

Do not commit incomplete generated attestations or a pending closure template. A negative completed
review may be committed as evidence, but it deliberately prevents authorization until the finding
is corrected and reviewed again against a newly pinned request.
