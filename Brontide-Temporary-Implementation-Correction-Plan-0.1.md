# Brontide Temporary Implementation Correction Plan 0.1

Status: Active temporary implementation request  
Scope: Reference stack, Minimal stack, interchange evidence, and repository engineering controls  
Authority: Non-normative. Architecture documents remain authoritative.

## 1. Purpose and lifetime

This plan records corrective work identified by the architecture review. It is intentionally
separate from the Reference and Minimal implementation roadmaps. Those roadmaps describe delivery
of Architecture 0.7; this document describes gaps that must be corrected before either stack can
claim reliable conformance.

Do not copy these corrections into the 0.3 roadmaps as completed requirements. Link to this plan
instead. Delete this file only after every completion gate in section 8 is satisfied and the
resulting evidence has been moved into the permanent milestone records.

No item in this document changes the architecture by itself. If implementation exposes an
architectural ambiguity, resolve it in the relevant architecture revision before coding a local
interpretation.

## 2. Finding A — Minimal Base authority model

The Minimal stack currently demonstrates a useful kernel, but its public model does not yet
establish several Base invariants strongly enough to support the existing conformance language.
References are publicly constructible, execution is not explicitly addressed to a target while
presenting authority, Capability records do not fully encode holder/target/constraint semantics,
and grant/step operations can be read as ambient authority.

Requested change:

1. Make issuer-controlled identities opaque at the public trust boundary. Callers may carry and
   compare issued references, but must not manufacture a reference that the kernel accepts as
   issued.
2. Represent an execution request as an explicit target plus an explicitly presented Capability.
   Validation must establish that the Capability authorizes that holder, target, operation, and
   constraint set.
3. Represent Capability provenance, holder, target, operation scope, and constraints in the model.
   Delegation must only narrow authority and append an auditable provenance link; it must never
   silently replace or widen the parent grant.
4. Remove or tightly encapsulate ambient grant and execution paths. A public helper must not bypass
   the same checks required of the primary execution path.
5. Give Genesis, clock/time progression, and Event attribution explicit rules and tests. Rejected
   attempts must be observable where Architecture 0.5 requires observability, without leaking
   protected payload data.
6. Align Minimal name/version behavior with the architecture: Version is not part of canonical
   identity unless the architecture explicitly says otherwise, and the parser must handle
   authority-qualified canonical names.

Required evidence:

- negative tests for forged references, wrong holder, wrong target, wrong operation, failed
  constraints, and widened delegation;
- positive tests for issue, delegate, execute, and event attribution paths;
- tests proving all public entry points enforce equivalent authority checks;
- a permanent milestone-evidence entry mapping each satisfied Base requirement to a test or
  reviewed implementation boundary.

## 3. Finding B — requirement-to-evidence traceability

The current tests are valuable regression tests, but prose milestone tables are not a stable
conformance matrix. Claims can drift when a document changes without a corresponding test or
evidence update.

Requested change:

1. Assign stable requirement identifiers to the normative requirements used for an implementation
   claim. Preserve aliases when wording moves between revisions.
2. Add a machine-readable or mechanically checkable matrix for each stack with: requirement ID,
   architecture revision, implementation component, positive evidence, negative evidence, status,
   and rationale for any non-applicable item.
3. Make the full verification gate fail on missing, duplicate, stale, or unreferenced requirement
   IDs.
4. Distinguish implemented, tested, demonstrated by interchange, planned, and not applicable.
   Passing a test must not automatically imply broad architectural conformance.
5. Treat the current milestone documents as narrative summaries generated from, or checked against,
   that matrix.

## 4. Finding C — breadth and measurement of interchange proof

The Cooling interchange proof establishes useful cross-process compatibility, but it covers one
Boolean operation, three Shapes, one Fragment, a single invocation pattern, no capability transfer,
and no referenced resources. The implementation-plan request to measure binding cost has not been
closed with recorded numbers.

Requested change:

1. Record reproducible generated-binding measurements for both stacks: source lines,
   generated/manual split, build contribution if practical, and the commands or method used.
2. Add a second interchange subject materially different from Cooling. It must exercise nested or
   repeated data, an explicit failure result, more than one operation, and at least one
   referenced-resource or authority-related boundary.
3. Add malformed, unknown-field/unknown-variant, version-skew, replay, and payload-limit vectors
   where the relevant protocol permits them.
4. Preserve independent implementations. Do not share generated runtime logic or a semantic adapter
   that could make both stacks fail identically.
5. Update the interchange README and milestone evidence with the exact limits of each proof.

## 5. Finding D — repository engineering controls

The local full gate passes, but maintainers do not yet have a repository-enforced
continuous-integration contract. The SDK is preview-based and unpinned by an explicit policy,
dependency-boundary checks are partly heuristic, and text-integrity repair has depended on manual
recovery.

Requested change:

1. Add CI that runs restore/build, the complete distinct test inventory, cross-process tests with
   required providers, dependency guards, documentation link checks, and UTF-8/text-integrity
   checks.
2. Pin the intended SDK or document and continuously test a supported SDK range. Make the
   FSharp.Core SDK-layout workaround explicit, bounded, and removable.
3. Replace heuristic-only dependency checks with project/assembly graph checks where possible;
   retain textual checks only as defense in depth.
4. Add a documented release/API compatibility policy and define what Architecture, wire, and
   package version numbers mean.
5. Add standard repository stewardship documents: license, contribution guidance,
   security-reporting guidance, and ownership/review expectations.

## 6. Finding E — maintainability, performance, and scope discipline

Several central files and generated bindings are large, there is no benchmark baseline, and the
published architecture surface is much broader than the validated core.

Requested change:

1. Set reviewable module boundaries around authority, execution, constraints, naming, bindings, and
   transport. Refactor only with behavior-preserving tests.
2. Establish small, repeatable benchmarks for constraint evaluation, serialization, cross-process
   invocation, and representative persistent-information operations.
3. Document payload limits, cancellation/timeouts, resource cleanup, event redaction, and
   denial-of-service assumptions at public boundaries.
4. Label features precisely as implemented, experimental, planned, or architectural. Avoid implying
   coverage of the broader architecture from the Base/Cooling proof.
5. Keep public APIs intentionally small; record why each exposed type or function must be public.

## 7. Delivery order

1. Freeze and publish the requirement-to-evidence vocabulary and current claim baseline.
2. Correct the Minimal Base authority model and its negative tests.
3. Establish CI, SDK policy, integrity checks, and stronger dependency verification.
4. Measure the existing interchange bindings and deliver the second interchange subject.
5. Refactor oversized boundaries and add benchmark/security-operability evidence.
6. Run independent architecture reviews of both stacks and reconcile only observable
   disagreements.

The 0.3 Architecture 0.7 implementation plans may begin with failing conformance vectors in
parallel, but neither stack may claim 0.7 implementation until the applicable corrections above
are closed.

## 8. Completion and deletion gate

This temporary plan may be deleted only when all of the following are true:

- every requested change is implemented or has a documented architecture-approved disposition;
- the full gate passes in CI and locally from a clean checkout;
- permanent requirement matrices and milestone evidence contain the resulting proof;
- both implementations have been reviewed independently against the same requirement IDs;
- README status language accurately reflects the evidence achieved;
- a review record names the commit that closes each finding and explicitly authorizes deletion.

Until then, this file is the controlling record for known corrective gaps. Deleting it is not
itself evidence that the gaps were fixed.

The checked [independent-review workflow](./conformance/reviews/README.md) makes these last review
conditions mechanically enforceable. `build/verify-independent-review.ps1` validates any review
records in progress; `build/verify-independent-review.ps1 -RequireComplete` requires two complete
stack attestations and an authorized closing record. If this temporary plan is absent, the verifier
enables that strict mode automatically as part of the full repository gate. These controls check
coverage and traceability but do not replace the reviewer's architectural judgment.

The reviewer may be automated under the pinned independence policy: it must use a distinct reviewer
identity, a fresh isolated context, no private implementation-session reasoning, and the complete
attestation. The attestation must consider the current architecture source and its delivery plan in
addition to retained implementation-baseline evidence. Automated review remains valid as the
architecture advances unless the current architecture or an explicit repository policy changes the
rule.
