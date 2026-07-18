# Independent implementation review

This directory contains the machine-checkable control plane for independent verification of the
Architecture 0.5 implementation correction. It does not turn conformance judgment into a script.
The scripts freeze the review target, generate complete evidence packets, check reviewer separation
statements, prevent missing requirement decisions, and make the temporary-plan deletion gate
enforceable.

The pinned request is [`review-request.json`](./review-request.json). It fixes the reviewed commit,
requirement vocabulary, per-stack evidence matrices, correction-closing commits, and the expected
record paths. Any change to a pinned matrix or vocabulary invalidates the request instead of
silently changing what the reviewer was asked to inspect. Verification needs the pinned Git objects
locally; CI therefore checks out full history. Record hashes use canonical UTF-8 text with LF line
endings so the same content has the same digest on Windows and CI checkouts.

## What requires a person

An independent reviewer must decide whether the implementation and its negative evidence actually
satisfy each requirement. A green build proves reproducibility, not architectural correctness. The
reviewer must not be an implementation actor named in the request and must disclose conflicts. One
reviewer may examine both stacks, but the stacks require separate packets and conclusions. Two
reviewers are preferable when available.

For every requirement, the reviewer reads the cited Architecture 0.5 section, inspects the source
and both positive and negative evidence, runs or extends tests where needed, and records one of:

- `conforms`;
- `approved-disposition`, with architecture-approved evidence;
- `does-not-conform`, which preserves the finding and blocks closure.

The framework checks that every applicable stable ID has exactly one reviewed decision. It cannot
prove that a reviewer was truthful or that their reasoning was competent; Git review controls or an
external signed review can strengthen identity assurance.

## Review workflow

Use a clean checkout for the pinned target and a separate checkout containing this framework. In
the framework checkout, generate one packet at a time:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\new-independent-review.ps1 `
  -Stack Reference -ReviewerId "github:reviewer" -ReviewerName "Reviewer Name"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\new-independent-review.ps1 `
  -Stack Minimal -ReviewerId "github:reviewer" -ReviewerName "Reviewer Name"
```

In the clean target checkout, detach at the `reviewTargetCommit` from `review-request.json`, run the
complete gate, and inspect the evidence listed in each packet:

```powershell
git switch --detach 69628a194834454169014b5b05dc8a6c2ad4d812
.\build\verify-interchange.ps1
```

Complete the generated JSON records without changing the generated requirement or evidence
snapshots. Set the gate result, review flags, rationale, verdict, overall verdict, timestamps, and
attestation fields. Then return to the framework checkout and run:

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
