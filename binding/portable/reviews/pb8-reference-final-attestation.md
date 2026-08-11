# PB8 Reference Portable Binding final attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-reference-final-review-2026-08-11`
- **Review date:** 2026-08-11
- **Pinned commit:** `fe299c71e2e77199ccdedfba552e193d3e0f91df`
- **Stack:** Reference only
- **PB8 step:** Step 5 final fresh-context review
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Implementation actor:** no
- **Isolation:** fresh local clone under the system temporary directory, checked out detached at the
  pinned commit. `HEAD` matched the complete pin and the snapshot was clean before review. One
  temporary assertion was added only in that isolated clone to execute the concrete counterexample
  recorded below; it was removed after the probe and the clone was clean again.
- **Retained review input:** the initial and first re-review Reference and neutral attestations were
  read only to identify findings whose closure had to be verified. No implementation-session private
  reasoning was available.

This record reviews the Reference realization and the neutral contract it implements. It does not
review Minimal correctness, repair findings, ratify Architecture 0.8 or Channel, or promote Portable
Binding beyond experimental version 0.1 evidence.

## Architecture, local target, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, exactly matching the registry.
The registry reports no latest ratified architecture. The registered hashes for the Reference 0.8
requirements, matrix, README, and milestone ledger also match their files.

The selected architecture keeps Composition, Component, Binding Plan, Portable Binding, Channel,
transport, provider selection, and lifecycle machinery outside Brontide Base. It requires
behavioural conformance, exact Shape identity/version and declared projection, strict authority
positions, explicit operational observations, fail-closed authority boundaries, implementation
independence, and evidence that does not substitute for specification or ratification. Portable
Binding remains a proposed first-party Component-interchange seam and its executed evidence does not
ratify it or Channel.

`Reference/README.md` states **Designed for: Brontide Architecture 0.8**, Complete Draft, not
ratified, with status **Partial implementation with explicitly labelled experiments**. The
Reference-native Portable Binding remains under
`Brontide.Reference.Experimental.Binding/Portable/`, outside Core. Its material limitations remain:

- version 0.1 is experimental, outside Base, and is not a stable public package or wire standard;
- the C6 floor exercises copied immutable blobs and addressing-only handles, but not borrowing,
  transferred ownership, lifetime/reuse, release signalling, or fallback;
- the PB7 handoff accepts only an already-resolved distinct `1..1` position and refuses discovery,
  acquisition, wider Provider Sets, mediation, generations, selection policy, and hot swap; and
- network security, federation, retries, cancellation, streaming, ordering, and exactly-once
  execution are not promised.

## C1-C10 decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and concrete evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact negotiation precedes plan creation and provider effects; provider identity is compared against the offered/answering provider; compact ids remain binding-scoped and post-negotiation. PB-78/PB-83 and establishment failure paths passed. |
| C2 | **conforms** | `PortableBindingPlan` is immutable, inspectable, and created only after negotiation. Provider facts come from the offered document, and the Composition handoff refuses before leaving a partial plan. Plan and handoff tests passed. |
| C3 | **conforms** | Reference evaluates local authority before emission, transports no Capability across trust, and makes denial frameless and effect-free. The authority/resource, establishment, parity, and process evidence passed. |
| C4 | **does-not-conform** | Envelope kinds, correlation categories, four-way result separation, process categories, and observer-relative failure domains are implemented, and PB-50 now truthfully reports an unknown effect count after peer termination. However PB-43 sends a request, receives a mismatched Outcome, and returns a protocol-error observation with effect count `0` although the vector explicitly declares the count unobservable. The isolated counterexample failed with expected null, actual zero. |
| C5 | **conforms** | The Reference Shape catalogue and codec preserve the declared scalar/composite floor, exact required fields and Fragments, additive payload projection, strict authority/control positions, and deterministic encodings. The neutral golden-encoding gate and native positive/adversarial tests passed. |
| C6 | **approved-disposition** | Inline values, copied immutable blobs, and addressing-only handles are implemented with scope, access, ownership, integrity, refusal, and copy-accounting evidence. Decision 9's explicitly unexercised borrow interval, lifetime/reuse, release, fallback, and transfer behaviours remain an approved narrowed 0.1 scope, not a broader conformance claim. |
| C7 | **conforms** | Direct and negotiated-process realizations remain independently implemented and compare the declared parity profile. The native suite passed, the focused Reference-process/neutral-provider run passed 29/29 without skips, Reference dependency direction passed, and the neutral provider resolved only two non-Brontide libraries. Agreement does not cure the C4/C9 contract violation above. |
| C8 | **conforms** | Explicit lifecycle transitions, bounds, replay protection, withdrawal, termination, and illegal-state handling remain fail closed. A late Outcome after withdrawal is a state violation, and PB-50 now records process loss without fabricating success or a known effect count. Native lifecycle and decoder evidence passed. |
| C9 | **does-not-conform** | The nullable representation, completeness rule, parity rendering, and PB-50 production loss path now preserve `unknown`. The general `PortableBindingHost.Rejected` path still passes literal `0` for protocol faults even when the request crossed the seam and attribution was lost. PB-43 is a concrete falsification of C9-P1 and the observation schema's rule that malformed correlation must not be rewritten as zero. |
| C10 | **conforms** | The neutral gate validated 84 unique vectors over C1-C10 and all 24 Channel vectors, rebuilt six golden encodings, built the implementation-neutral provider, and found no Brontide assembly in its two resolved libraries. Reference native portable evidence passed 189 tests; the focused Reference-provider and neutral-provider process evidence passed 29/29. |

## Prior finding dispositions

| Finding | Disposition at `fe299c7` | Evidence |
| --- | --- | --- |
| Reference F1 / neutral N1 - stale Architecture 0.7 target text | **closed** | The selected architecture, contract matrix, PB8 plan, public-boundary policy, neutral-provider boundary, and Reference target consistently state Architecture 0.8 while preserving Complete Draft and non-ratified status. The neutral gate's target checks passed. |
| B1 - contradictory provider identity and plan-source rules | **closed** | Negotiation refuses provider mismatch and all plan provider facts name the offered/answering provider. PB-78/PB-83 and the Reference negotiation/handoff tests passed. |
| B2 - impossible post-withdrawal Outcome acceptance | **closed** | The contract and Reference lifecycle have no withdrawn-to-Outcome transition; PB-84 requires and the native test returns `state-violation`. |
| B3 - mandatory fabricated effect count | **partly closed for the Reference realization** | The neutral optional/unknown representation is closed, and the prior Reference process-loss finding is closed: `Lost` passes null and PB-50 reaches that production path successfully. The underlying attribution class remains in `Rejected`, where PB-43 still fabricates zero after correlation is lost; see R1. |
| B4 - no capability-wide properties | **declaration closed; C9-P1 false in Reference** | The neutral corpus declares exactly one `all-vectors-with-capability` property with a concrete counterexample for each C1-C10, and the gate enforces their presence and shape. The PB-43 probe realizes C9-P1's forbidden fabricated-zero class, so declaration does not establish runtime truth. |
| N2 - stale Channel execution metadata and contradictory ledger preface | **closed** | Channel metadata says both stack harnesses executed the vectors while retaining non-ratification. CH-R11 is `realisation-executed`, and the forward-scenario preface now distinguishes delivered Portable Binding evidence from remaining targets. The neutral gate's Channel-ledger check passed. |
| N3 - current public policy points new decisions to Architecture 0.7 | **closed** | `docs/current/policies/public-boundaries.md` now directs new decisions to the current Architecture 0.8 and explicitly preserves its Complete Draft, non-ratified status. The neutral gate contains and passed a regression check for the stale phrase. |

## Finding

### R1 - blocking - protocol rejection still fabricates zero provider effects (C4, C9)

`binding/portable/vectors/limits-lifecycle-and-channel.json` states for
`PB-43-CORRELATION-MISMATCH` that an Outcome claiming a request the host did not send cannot have an
effect count attributed to it. `binding/portable/schemas/binding-observation.json` likewise requires
`unknown` when malformed correlation prevents attribution, and C9-P1 names fabricated zero after
loss of evidence as its counterexample.

The Reference host sends the request and then `PortableProcessConversation.RequestAsync` detects the
mismatched Outcome. `PortableBindingHost.InvokeAsync` catches the resulting `PortableFaultException`
and calls `Rejected`; that method passes literal `0` to `PortableObservationBuilder.Build` for every
protocol rejection. Consequently the normal PB-43 production path reports a known zero instead of
unknown. An assertion added temporarily to the existing PB-43 real-process test failed exactly as
predicted: `Expected: null; But was: 0`.

This is not the corrected PB-50 path: `Lost` now passes null, the named PB-50 test asserts null, and
that test passes. The remaining defect is the same knowledge-fabrication class at a different
terminal path. It also shows why a green vector-coverage name mapping cannot substitute for asserting
the vector's effect-attribution expectation.

No other blocking Reference implementation or documentation finding was identified.

## Commands and results

All inspection and execution commands ran in the isolated detached clone unless stated otherwise.

| Command | Result |
| --- | --- |
| `git clone --no-hardlinks <repository> <system-temp>; git checkout --detach fe299c71e2e77199ccdedfba552e193d3e0f91df; git rev-parse HEAD` | Passed; HEAD exactly matched the pin and the snapshot was clean. A command-scoped `safe.directory` exception was required for the sandbox reviewer identity. |
| Registry SHA-256 comparison for the selected architecture and Reference current-delivery requirements, matrix, README, and ledger | Passed; all five registered hashes matched. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly` | First run: all neutral data checks passed, then restore failed with `NU1301` because sandbox network access was denied. Approved rerun passed: 9 schemas, 84 vectors, C1-C10, all 24 Channel vectors, 6 re-derived golden encodings; neutral provider built with 0 warnings/0 errors; 2 resolved libraries and no Brontide assembly. |
| `dotnet test .\Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj -nologo --filter "FullyQualifiedName~.Portable."` | Passed 189, failed 0, skipped 44. Skips were environment-gated foreign-process and provider-path cases. |
| Targeted unmodified PB-50 run, `--filter "FullyQualifiedName~A_peer_that_ends_between_frames"` | Passed 1/1. The real local seam closes the provider output after the request and the observation asserts an unknown effect count. |
| Build `Brontide.Reference.Interchange.Provider`, set `BRONTIDE_REFERENCE_PROVIDER` and `BRONTIDE_NEUTRAL_PROVIDER`, then run `PortableCrossProcessTests|PortableNeutralProviderTests` with `--no-build --no-restore` | Provider build passed with 0 warnings/0 errors; focused process evidence passed 29, failed 0, skipped 0. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1` | Passed; 21 project references checked and Reference dependency direction was valid. |
| Add only in the isolated clone an assertion that PB-43's mismatched-Outcome observation has unknown provider effect count, then run that one existing real-process test | Failed as predicted: expected null, actual `0`. The temporary assertion was removed; the isolated clone returned clean and the shared worktree received no implementation change. |
| Targeted reads and searches of AGENTS.md, the status-selected Architecture 0.8, Reference target/status/limitations, PB8 review policy, retained initial/re-review findings, the complete neutral schema/vector corpus, Channel ledger/status, Reference Portable production paths, and native coverage/parity/lifecycle/observation tests | Completed. |

The environment selected .NET SDK `10.0.400-preview.0.26322.102`; NETSDK1057 was informational.

## Overall verdict

**does-not-conform**

The pinned commit closes the prior PB-50 process-loss defect and all retained documentation findings,
and its standard neutral, native, process, and dependency gates pass. PB8 Step 5 still cannot close
for the Reference realization because PB-43 proves that protocol rejection after correlation loss
fabricates a zero provider-effect count. C4 and C9 therefore do not conform, B3 is not fully closed in
the Reference runtime, and C9-P1 is false on a reachable production path.
