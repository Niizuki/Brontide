# PB8 Minimal independent attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-minimal-review-2026-08-11`
- **Review date:** 2026-08-11
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Implementation reviewed:** Brontide Minimal Stack only
- **Pinned commit:** `ab94ad742104f6939b4a378373f2d68b285c3751`
- **Snapshot:** fresh detached local clone in a system temporary directory; the shared mutable
  worktree was not used as review evidence
- **Other reviewers' output:** not read

This reviewer did not implement the Portable Binding work and had no access to the implementation
session's private reasoning. The review covered the status registry, the complete current
Architecture 0.8 document, `Minimal/README.md`, the Portable Binding plan, neutral contract matrix,
schemas and vectors, Minimal's native Portable Binding implementation, and its positive and negative
tests. The review does not inspect or attest the Reference implementation.

## Architecture and local target

- **Current architecture:** Architecture 0.8, `Complete Draft (document and implementation evidence
  complete; not ratified)`, selected by `Brontide-Architecture-Status.json` at
  `docs/current/architecture/Brontide-Architecture-0.8.md`.
- **Registry digest:** `6D844F5FA4D0D3CF09188765A912A13B30889A6ED1F232A28351F179016A8B2F`.
- **Latest ratified architecture:** none. The registry explicitly records `revision: null`,
  `status: none`, and no path or digest.
- **Minimal local target:** `Minimal/README.md` states `Designed for: Brontide Architecture 0.8`,
  Complete Draft, not ratified, and describes the stack as a partial implementation with explicitly
  labelled experiments. The status registry independently records Minimal `designedFor: 0.8`.
- **Portable Binding status:** planned experimental work designed for Architecture 0.8 sections 16
  and 18.1; it is not ratified and is not part of Brontide Base.
- **Target consistency verdict:** **does-not-conform** because the authoritative local target and
  registry agree on 0.8, while retained current/PB8-facing documents still claim a 0.7 target. This
  is Finding F1 below; no later draft rule was projected into an older implementation by this review.

Architecture 0.8's relevant constraints are preserved by the reviewed surface: Portable Binding is
Composition work in progress outside Base; a frozen Binding Plan fixes contracts, authority
presentation, representation, ownership, synchronization, delivery and lifecycle before the hot
path; representation mapping stays within one Shape contract; semantic conversion requires an
Adapter Component; Components are not Actors and grant no authority; payload compatibility is
additive while authority/control positions fail closed; no Capability crosses a trust boundary;
provider, crossed-boundary, copy, failure-domain and lifecycle facts remain observable. The current
architecture's own status remains Complete Draft and the review makes no ratification claim.

## Known limitations and approved boundaries

- Version 0.1 exercises copied immutable blobs and retained addressing-only handles. Borrow
  interval, lifetime, release signal and fallback policy are declared but unexercised; borrow and
  transfer are explicit 0.1 non-goals.
- The seam does not promise network security, identity federation, exactly-once delivery, retries,
  cancellation, streaming or long-running lifecycle semantics.
- Portable Binding, Channel and Composition remain experimental/unratified and outside Base.
- The ordinary Minimal test run deliberately skips foreign-process cases unless the relevant built
  provider paths are supplied. This review built and checked the implementation-neutral provider
  and ran all ungated Minimal tests, but did not run the Reference provider or claim a new live
  cross-stack execution.

## Capability verdicts

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and pinned evidence |
| --- | --- | --- |
| C1 — neutral contract establishment | **conforms** | The neutral schemas and PB-01–PB-09/PB-54/PB-83 require canonical, versioned establishment before readiness or effect, reject unknown versions, identities, fields and variants, scope compact ids to the frozen plan, and verify the provider that answered. Minimal's establishment and fixture-alignment tests exercise both positive and fail-closed paths; the ungated suite passed. |
| C2 — complete Binding Plan | **conforms** | `binding-plan.json` fixes the complete immutable plan. PB-01, PB-53, PB-54 and PB-83 plus `PortablePlanObservationAndParityTests` and `PortableCompositionHandoffTests` show that plan facts are inspectable before invocation, compact ids remain plan data, provider identity is answered rather than requested identity, and preflight does not fabricate a plan before Interconnection. |
| C3 — authority remains local | **conforms** | `authority-presentation.json`, PB-18–PB-24, PB-56 and PB-59 require frameless local denial, zero provider effects, fail-closed strong-Kleene authority evaluation and rejection of Capability-shaped trust-crossing bodies. Minimal's authority/resource, lifecycle-seam and parity tests passed those negative paths. |
| C4 — Channel 0.1 semantics | **conforms** | The envelope schema and PB-33–PB-52 preserve the declared kinds, echoed correlation, protocol/process distinction, failure domains, shaped failed Outcomes and forbidden private-runtime content. Minimal's channel-vector coverage proves every one of the 24 Channel vectors is executed or accounted for; taxonomy, lifecycle/channel, process-category and decoder-property tests passed locally. |
| C5 — portable shaped values | **conforms** | `references-and-shape-floor.json` and PB-10–PB-17/PB-57 cover scalar, record, sequence and choice values, required fields, open/closed fragments, nested/repeated values, additive payload projection and strict authority/control positions. Minimal's schema-guided CBOR codec and Shape catalog implement those distinctions; encoding, decoder-property, fixture-alignment and Catalog tests passed. |
| C6 — inline and referenced payloads | **approved-disposition** | Inline values, copied immutable blobs and addressing-only handles are implemented and tested through PB-25–PB-32/PB-60, including integrity, scope, bounds, forbidden implicit copies and zero-octet handles. The contract matrix explicitly approves the narrower 0.1 floor: borrow interval, lifetime, release signal and fallback are declared but unexercised, while borrow and transfer remain non-goals. That disclosed limitation is accepted for PB8 and is not presented as broader evidence. |
| C7 — realization independence and parity | **conforms** | Minimal owns its F# implementation under `Brontide.Minimal.Binding/Portable`; its project graph references Minimal Model/Kernel and neutral data, not Reference assemblies or private types. PB-58–PB-60/PB-62, realization-parity tests and dependency/data-only guards compare fixed direct and negotiated process observations while permitting only declared realization differences. The fresh ungated Minimal parity evidence passed; pinned cross-process tests remain executable but provider-path gated. |
| C8 — bounded and explicit lifecycle | **conforms** | `limits-and-lifecycle.json`, PB-09 and PB-33–PB-41 define establishment, readiness, request, withdrawal and termination plus frame, payload, nesting, field, resource, concurrency, replay and timeout bounds. Minimal's lifecycle/channel and lifecycle-seam negative tests passed: illegal state, replay, mismatch, interruption and timeout do not fabricate success or provider effects. |
| C9 — attributable observations | **conforms** | `binding-observation.json`, PB-53/PB-55/PB-56 and the parity profile require provider, reason, negotiated identities, representation, boundaries, copies/resources, authority point, mapping, retries, interruption, failure domain, terminal/correlation/timing and provider-effect observations. Minimal's plan/observation/parity tests and all failure-path leakage properties passed; diagnostics remain non-semantic. |
| C10 — executable interoperability evidence | **conforms** | The pinned suite contains native direct, same-stack process, Minimal-host/Reference-provider, and implementation-neutral-provider fixtures over the shared vectors. The fresh neutral gate rebuilt the neutral provider and verified its resolved dependency graph contains no Brontide assembly. The fresh Minimal run passed all 184 ungated tests; the 52 provider-path-gated cases were skipped rather than misreported as fresh executions. Existing PB5 evidence remains discoverable, while this attestation makes no new cross-stack run claim. |

## Commands and results

Commands were run from the detached temporary snapshot at the pinned commit.

1. `git clone --no-local --no-checkout <local-repository> <system-temp>; git checkout --detach ab94ad742104f6939b4a378373f2d68b285c3751`
   — passed; `HEAD` exactly matched the pinned commit and the initial snapshot was clean.
2. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   — the first sandboxed attempt completed neutral validation, then failed only because NuGet network
   access was denied. The approved rerun passed: 9 neutral schemas, 83 vectors covering C1–C10 and
   all 24 Channel vectors, 6 re-derived golden encodings, neutral-provider build success, and 2
   resolved libraries with none from either Brontide stack.
3. `dotnet test .\Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj -nologo`
   — bounded attempt timed out after 184 seconds while build/test setup was still running; no pass or
   failure was inferred from the timeout.
4. `dotnet test ... --no-restore --list-tests`
   — passed after completing the Minimal test build and enumerated the portable positive, negative,
   property, parity, cross-process, cross-stack and neutral-provider tests.
5. `dotnet test ... -nologo --no-build --no-restore`
   — passed in 45 seconds: **184 passed, 0 failed, 52 skipped, 236 total**. Skips were the expected
   provider-path-gated foreign/process cases.
6. `dotnet build .\Minimal\src\Brontide.Minimal.Interchange.Provider\Brontide.Minimal.Interchange.Provider.fsproj -nologo`
   — passed with 0 warnings and 0 errors.

## Findings

### F1 — blocking: PB8 target statements contradict the authoritative Minimal target

`Brontide-Architecture-Status.json` and `Minimal/README.md` both state that Minimal is designed for
Architecture 0.8. The current Architecture 0.8 introduction still says both stacks target 0.7;
`binding/portable/contract-matrix.md` says its evidence changes neither stack's 0.7 target;
`binding/portable/README.md` repeats that boundary; and PB8 Step 5 in the active plan instructs
reviewers to respect a stated 0.7 target. These claims cannot all be true at the pinned commit.

This is blocking for PB8 evidence/documentation/review closure because the independent-attestation
rule requires review of the current architecture and the implementation's locally stated target and
limitations, while PB8's exit requires those limitations and evidence to be current. It does not
invalidate the C1–C10 executable behavior, and it does not ratify Architecture 0.8, but the conflicting
target text must be reconciled before this review can attest local-target consistency.

No other blocking or nonblocking Minimal finding was identified from the inspected artifacts and
executed evidence.

## Overall verdict

**does-not-conform for PB8 Step 5 closure at the pinned commit.** C1–C5 and C7–C10 conform; C6 has
an explicit approved disposition for the narrower 0.1 resource floor. The executable Minimal and
neutral evidence inspected here is green, but Finding F1 prevents an honest attestation that the
PB8 documentation consistently states the implementation target. This verdict does not allege an
Architecture 0.8 Base-conformance failure and does not expand the experimental Portable Binding
claim.
