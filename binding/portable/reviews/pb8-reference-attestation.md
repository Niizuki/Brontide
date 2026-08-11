# PB8 Reference Portable Binding attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-reference-review-2026-08-11`
- **Review date:** 2026-08-11
- **Pinned commit:** `ab94ad742104f6939b4a378373f2d68b285c3751`
- **Stack:** Reference only
- **PB8 step:** Step 5, fresh independent review
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Isolation:** the pinned commit was checked out in a fresh local clone under the system temporary directory. The shared mutable worktree was used only to write this attestation.
- **Other reviewer output:** not read.

## Architecture and local target

`Brontide-Architecture-Status.json` selects Architecture 0.8 at
`docs/current/architecture/Brontide-Architecture-0.8.md`, whose pinned SHA-256 was reproduced as
`6D844F5FA4D0D3CF09188765A912A13B30889A6ED1F232A28351F179016A8B2F`. Its status is **Complete
Draft (document and implementation evidence complete; not ratified)**. The registry records no
latest ratified architecture: revision and path are null and status is `none`.

The complete selected architecture was reviewed, including its Base boundary, explicit and bounded
authority, payload/authority variance, Shape identity/version/projection rules, semantic-portability
observations, experimental Composition/Component direction, conformance and openness rules,
implementation independence, external trust admission, and the 0.8 change record. Portable Binding
is consistent with that architecture as an experimental Channel realization outside Base: it does
not move transport, provider selection, or Composition into Core; it preserves strict authority
positions and additive payload projection; it exposes crossed operational boundaries; and its
Reference implementation depends on Reference Core rather than either Minimal runtime or shared
semantic code.

`Reference/README.md` states **Designed for: Brontide Architecture 0.8**, Complete Draft, not
ratified, with status **Partial implementation with explicitly labelled experiments**. That target
matches the status registry and the plan's resolved 2026-08-10 Architecture target closure. The
Portable Binding implementation is correctly located in
`Brontide.Reference.Experimental.Binding/Portable/`, and the README does not present it as Base,
ratified, or a public-package conformance claim.

## Capability decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and concrete evidence |
| --- | --- | --- |
| C1 | **conforms** | `PortableNegotiation`, `PortableContractCodec`, canonical typed references, and the establishment path negotiate the versioned contract before readiness or an operation effect. PB-01 through PB-09, PB-54, PB-74, PB-78, and PB-83 cover exact establishment, unknown/skewed contract data, premature compact ids, preflight mismatch, and provider substitution. The native suite passed these positive and fail-closed paths. |
| C2 | **conforms** | `PortableBindingPlan` is a consolidated immutable record created by negotiation and inspected without invoking the provider. `PortableCompositionHandoff` carries the resolved requirement and offered provision into preflight while preserving binding-scope facts across lifecycle stages. PB-01, PB-39, PB-53, PB-54, and PB-72 through PB-82 plus HANDOFF-P1/P3 have named Reference tests. |
| C3 | **conforms** | `PortableBindingHost` performs local authority evaluation and scans for Capability-shaped content before emission; `PortableProviderEndpoint` repeats the no-Capability scan defensively. Local denial is frameless and effect-free. PB-18 through PB-24, PB-56, PB-59, CATALOG-P2, and the authority/resource and parity suites exercise true, false, unknown, missing declaration, missing Fragment, and attempted Capability transfer. |
| C4 | **conforms** | The Reference-owned envelope, framing, conversation, taxonomy, and observation code distinguishes shaped failed Outcome, protocol error, process failure, and frameless denial, with correlation and observer-relative failure domains. `PortableChannelVectorCoverageTests` derives coverage from the neutral declarations; the neutral gate covered all 24 Channel vectors and the native tests cover PB-42 through PB-52 plus PB-16/PB-17. Runtime exceptions are reduced to portable categories rather than transported. |
| C5 | **conforms** | The Shape catalogue/value codec implements the declared scalar, record, sequence, choice, Fragment, required-field, open/closed, additive payload projection, and strict authority/control floor. Six golden encodings were independently re-derived by the neutral gate. PB-10 through PB-17, PB-19, PB-24, PB-57 and Catalog nested/detail vectors exercise both acceptance and rejection. |
| C6 | **approved-disposition** | The implemented 0.1 floor supports inline values, copied immutable blobs with SHA-256 integrity, and addressing-only handles with explicit scope and zero-octet behavior. PB-25 through PB-32, PB-60, PB-68/PB-69, the seam tests, and CATALOG-P2 cover positive and adversarial behavior. Decision 9 explicitly accepts that borrow interval, lifetime, release/completion signal, and fallback policy are declared but unexercised because the two 0.1 flavors cannot represent those paths; both stacks pin the flavor set so widening forces review. This is an approved scope disposition, not evidence for those future behaviors. |
| C7 | **conforms** | Direct and negotiated-process conversations are separate Reference realizations over the same contract. Thirteen data-defined scenarios cover every host-reachable result class, compare the normative parity profile, check effect counts, and assert failure-path non-leakage. The native suite passed; the separately launched Reference provider and neutral-provider run also passed 29 real-process tests. Dependency checks found no stack runtime in the neutral provider. |
| C8 | **conforms** | `PortableLifecycle` explicitly models unestablished, establishing, established, ready, active, withdrawn, terminated, and failed states; illegal transitions fail closed. Frame/payload/depth/field/resource/replay limits are declared and enforced before uncontrolled work. PB-09 and PB-31 through PB-41, lifecycle seam tests, decoder properties, and PB7 release-barrier tests cover establishment failure, pre-ready/post-withdrawal requests, replay, timeout/interruption, and bounds without fabricating success. Retry, cancellation, ordering, streaming, and exactly-once execution remain explicit non-promises. |
| C9 | **conforms** | `PortableObservation` records the normative provider, reason, negotiated references, representation, boundaries, copies/resources, authority point/decision, mapping obligations, retry/interruption, failure domain/status, correlation, timing, and observable provider-effect count. PB-55 through PB-57, PB-60, PB-71 and the plan/observation, resource, Catalog, and parity tests cover success, denial, mapping, copy differences, and corrected refusal facts. Diagnostics/local codes are excluded from portable semantics. |
| C10 | **conforms** | The neutral gate validated all 83 vectors and built the implementation-neutral provider, then verified its resolved dependency graph contained 2 libraries and no Brontide assembly. Reference native evidence passed 187 tests; a focused run against the built Reference process endpoint and neutral provider passed 29/29 with no skips. Reference also contains executable `PortableCrossStackTests` for a Minimal provider and vector-coverage guards that permit no deferred neutral vectors. The Minimal-provider direction was inspected but not re-executed in this Reference-only review. |

## Known limitations and dispositions

- Portable Binding 0.1 remains experimental, outside Brontide Base, and does not ratify Architecture
  0.8, Channel, Composition, or a final public package/wire standard.
- C6's copied-blob/addressing-handle floor does not exercise borrowing, lifetime expiry, release
  signalling, fallback, or transferred ownership; this is the approved C6 disposition above.
- The PB7 Composition handoff is limited to distinct `1..1` exposure. Wider Provider Sets,
  mediation, discovery/resolution, acquisition, generations, and hot swap remain Component
  Management work and are refused rather than approximated.
- Network security, federation, retries, cancellation, streaming, ordering, and exactly-once
  execution are not promised by version 0.1.
- `copyCount` counts referenced-resource copies; an inline message is not reported as a resource
  copy.

## Findings

### F1 — nonblocking documentation target drift

Three retained current statements still say the stacks target Architecture 0.7:

1. the introductory target paragraph in `docs/current/architecture/Brontide-Architecture-0.8.md`;
2. the status paragraph in `binding/portable/contract-matrix.md`; and
3. PB8 Step 5 in `docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md`.

They conflict with `Brontide-Architecture-Status.json`, `Reference/README.md`, the plan's goal
boundary, and its resolved 2026-08-10 Architecture target closure, all of which say the Reference
stack now targets 0.8. This does not invalidate C1-C10 behavior or the Reference implementation
attestation, so it is nonblocking for the capability verdicts. It should be corrected during PB8
documentation closure so reviewers are not instructed to respect a superseded target.

No blocking Reference implementation or contract finding was identified.

## Commands and results

All inspection and execution commands below ran in the fresh temporary clone at the pinned commit.

| Command | Result |
| --- | --- |
| `git clone --no-hardlinks <repository> <temp>; git checkout ab94ad742104f6939b4a378373f2d68b285c3751; git rev-parse HEAD` | Passed; HEAD exactly matched the pin. A command-scoped `safe.directory` exception was required for the sandbox-owned reviewer process. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly` | First run: neutral artifact validation passed, then NuGet restore failed because sandbox network access was denied. Retried with approved network access: passed completely — 9 schemas, 83 vectors, C1-C10, all 24 Channel vectors, 6 re-derived golden encodings; neutral provider built with 0 warnings/0 errors; dependency check found 2 resolved libraries and no Brontide assembly. |
| `dotnet test .\Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj -nologo --filter "FullyQualifiedName~.Portable."` | First bounded attempt timed out during restore/build without a test result. Retried with `--no-restore`: passed 187, failed 0, skipped 44. The skips were the provider-dependent real-process, cross-stack, and neutral-provider cases because their environment paths were not set for that run. |
| Build Reference provider, set `BRONTIDE_REFERENCE_PROVIDER` and `BRONTIDE_NEUTRAL_PROVIDER`, then `dotnet test ... --no-build --no-restore --filter "FullyQualifiedName~PortableCrossProcessTests|FullyQualifiedName~PortableNeutralProviderTests"` | One combined build/test attempt was stopped without a result after exceeding the review wait window; the provider build had completed. The focused no-build rerun passed 29, failed 0, skipped 0. |
| Source/test inspection with `rg`, JSON parsing, SHA-256 checks, and targeted reads of the complete selected architecture, Reference target/status/limitations, PB plan, neutral schemas/vectors, Reference Portable implementation, and positive/negative tests | Completed. Other reviewer attestations were not opened. |

The environment selected `.NET SDK 10.0.400-preview.0.26322.102`; SDK preview notice NETSDK1057 was
informational and the successful builds reported zero warnings and zero errors.

## Overall verdict

**Conforms with one approved capability disposition and one nonblocking documentation finding.**

The pinned Reference Portable Binding implementation provides adequate independent experimental
evidence for C1-C10. C6 is approved only at its explicitly narrowed 0.1 resource floor. No blocking
implementation defect was found. F1 should be repaired before PB8 documentation is declared fully
closed, but it does not change the Reference capability verdicts.
