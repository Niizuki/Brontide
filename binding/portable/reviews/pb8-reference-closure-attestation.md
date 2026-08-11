# PB8 Reference Portable Binding closure attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-reference-closure-review-2026-08-11`
- **Review date:** 2026-08-11
- **Pinned commit:** `5150d6d774d683a6ce8e769f7472724d40f0baba`
- **Stack:** Reference only
- **PB8 step:** Step 5 closure review
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Implementation actor:** no
- **Isolation:** the complete pin was checked out detached in a fresh local clone under the system
  temporary directory. `HEAD` matched the pin and the isolated snapshot remained clean. The shared
  mutable worktree was used only to write this attestation.
- **Retained review input:** the PB8 Reference and neutral initial, re-review, and final
  attestations were read only as finding history for B1-B4, N1-N3, and R1. No implementation
  session's private reasoning was available.

This record reviews the Reference realization and the neutral requirements it implements. It does
not review Minimal correctness, ratify Architecture 0.8 or Channel, or promote Portable Binding
beyond experimental version 0.1 evidence.

## Architecture, local target, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The complete selected document was
reviewed. Its SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, exactly matching the
registry. The registry reports no latest ratified architecture. The registered hashes for the
Reference Architecture 0.8 requirements, current-delivery matrix, README, and milestone ledger also
match their files.

The selected architecture keeps Composition, Component, Binding Plan, Portable Binding, Channel,
transport, provider selection, and lifecycle machinery outside Brontide Base. It requires
behavioural conformance, exact Shape identity/version and declared projection, strict authority
positions, explicit operational observations, fail-closed authority boundaries, and implementation
independence. Portable Binding remains a proposed first-party Component-interchange seam; executed
evidence neither ratifies it nor moves it into Base.

`Reference/README.md` states **Designed for: Brontide Architecture 0.8**, Complete Draft, not
ratified, with status **Partial implementation with explicitly labelled experiments**. The
Reference-native implementation remains in
`Brontide.Reference.Experimental.Binding/Portable/`, outside Core. Its material limitations remain:

- Portable Binding 0.1 is experimental and is not a stable public package or wire standard.
- C6 exercises copied immutable blobs and addressing-only handles, but not borrowing, transferred
  ownership, lifetime/reuse, release signalling, or fallback.
- The PB7 handoff accepts only an already-resolved distinct `1..1` position. Discovery,
  acquisition, wider Provider Sets, mediation, generations, provider-selection policy, and hot swap
  remain out of scope and are refused rather than approximated.
- Network security, federation, retries, cancellation, streaming, ordering, and exactly-once
  execution are not promised.
- Relational Initialisation remains refused in 0.1; Decision 13 assigns the wider lifecycle
  contract to 0.2.

## C1-C10 decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and concrete evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact negotiation precedes plan creation and provider effects. Provider identity is compared with the offered/answering provider, unknown contract material fails closed, and compact ids exist only after negotiation within one binding. PB-78/PB-83 and the establishment tests passed. |
| C2 | **conforms** | `PortableBindingPlan` is immutable, inspectable, and created only after successful negotiation. Its provider facts come from the offered document, and failed preflight leaves no partial plan. Native plan and handoff evidence passed. |
| C3 | **conforms** | Reference evaluates authority locally before emission, forbids Capability content across trust, and keeps denial frameless and effect-free. Authority/resource, establishment, parity, and process evidence passed. |
| C4 | **conforms** | Envelope kinds, correlation, protocol/process categories, four-way result separation, and observer-relative failure domains are explicit. The neutral gate covered all 24 Channel vectors. The repaired host preserves unknown effects for post-request correlation, malformed/unknown-frame, state, and internal-protocol failures; PB-43 now exercises the corrected production path. |
| C5 | **conforms** | The Shape catalogue and codec preserve the declared scalar/composite floor, required fields and Fragments, additive payload projection, strict authority/control positions, and deterministic encodings. The neutral golden-encoding gate and native positive/adversarial tests passed. |
| C6 | **approved-disposition** | Inline values, copied immutable blobs, and addressing-only handles have scope, access, ownership, integrity, refusal, and copy-accounting evidence. Decision 9's borrowing, transfer, lifetime/reuse, release, and fallback omissions remain an explicitly narrowed 0.1 disposition, not a broader conformance claim. |
| C7 | **conforms** | Direct and negotiated-process realizations remain independent and compare the declared parity profile. Native evidence passed; the Reference-process and neutral-provider run passed 29/29 without skips; the dependency gate passed; and the neutral provider resolved only two non-Brontide libraries. |
| C8 | **conforms** | Explicit lifecycle transitions, limits, replay protection, withdrawal, termination, and illegal-state handling remain fail closed. PB-84's post-withdrawal Outcome is a state violation whose effect remains unknown after seam entry. Native lifecycle and decoder evidence passed. |
| C9 | **conforms** | `ProviderEffectCount` is nullable, success requires a known count, and terminal observations render absence as `unknown`. Production classification now combines whether a request entered the seam with whether the failure destroys attribution: all reachable unknown-effect classes remain unknown, while a pre-provider rejection such as PB-46 remains known zero. PB-43/PB-46/PB-50 passed together. |
| C10 | **conforms** | The neutral gate validated 84 unique vectors over C1-C10 and all 24 Channel vectors, rebuilt six golden encodings, built the implementation-neutral provider, and found no Brontide assembly in its two resolved libraries. Reference native Portable evidence passed 189 tests, and focused real-process evidence passed 29 tests without skips. |

## Effect-attribution defect-class review

The neutral corpus has ten vectors with `effectCountNotAsserted`. The Reference production paths
preserve each vector's absence of attributable knowledge:

| Neutral vector | Reachable Reference host classification |
| --- | --- |
| PB-40 I/O timeout | Every `PortableProcessFailureException`, including timeout, reaches `Lost`, which records `null`. |
| PB-41 interrupted frame | Transport interruption reaches `Lost`, which records `null`. |
| PB-43 correlation mismatch | The request has entered the seam and `CorrelationMismatch` is attribution-destroying, so `Rejected` receives `null`; the named real-seam regression test asserts this. |
| PB-44 missing correlation identity | A malformed response decoded after request emission raises `MalformedMessage`; the post-seam classification records `null`. |
| PB-45 unknown envelope kind | An unknown response kind raises `UnsupportedKind`; the post-seam classification records `null`. |
| PB-49 internal protocol failure | `InternalProtocolFailure` after request emission records `null` in both direct and process conversations. |
| PB-50 peer terminated | Peer termination reaches `Lost`, which records `null`; the named real-seam regression test asserts this. |
| PB-51 process-category selection | All seven process categories share the `Lost` path and therefore preserve an unknown count rather than inventing one category-wide count. |
| PB-52 failure-domain relativity | Changing the observer-relative domain does not invent an effect count; the process-failure path remains `null`. |
| PB-84 late Outcome after withdrawal | `StateViolation` after the request entered the seam is attribution-destroying and records `null`. |

The classification is deliberately two-dimensional. `requestEnteredSeam` becomes true immediately
before `RequestAsync`; only after that point do correlation mismatch, malformed message, unsupported
kind, state violation, and internal protocol failure become unattributable. Every process failure is
unattributable through `Lost`. Before seam entry, `Rejected` receives known zero even for a category
that could be unattributable later. PB-46 proves that boundary: an unsupported Operation is rejected
while preparing the request, and its observation remains zero. The focused three-test run therefore
pins the class at its decisive edges: PB-43 unknown, PB-46 zero, and PB-50 unknown.

The prior R1 diagnosis was itself executed before the repair: the retained final Reference review
temporarily asserted PB-43's production observation and observed expected `null`, actual `0`. The
pinned change adds that assertion to the permanent named test and changes the production
classification rather than constructing a synthetic observation.

## Finding dispositions

| Finding | Closure disposition at `5150d6d` | Evidence |
| --- | --- | --- |
| B1 - contradictory provider identity and plan-source rules | **closed** | Negotiation refuses provider mismatch and plan provider facts name the offered/answering provider. PB-78/PB-83 and native negotiation/handoff evidence passed. |
| B2 - impossible post-withdrawal Outcome acceptance | **closed** | No withdrawn-to-Outcome transition exists; PB-84 requires `state-violation`, and post-seam classification preserves an unknown effect count. |
| B3 - mandatory/fabricated zero effect count | **closed for Reference** | Nullable observation representation, `Lost`, and post-seam protocol-failure classification now preserve unknown effects. The complete ten-vector inventory and PB-43/PB-46/PB-50 boundary run establish the Reference disposition. |
| B4 - no capability-wide properties | **closed** | The neutral corpus has exactly one `all-vectors-with-capability` property with a concrete counterexample for each C1-C10; the neutral gate enforces the declarations, and the repaired Reference paths no longer falsify C9-P1. |
| N1 - stale Architecture 0.7 target text | **closed** | Registry, selected architecture, contract matrix, Portable Binding plan/policy, neutral-provider boundary, Reference README, and milestone ledger consistently preserve the Architecture 0.8 Complete Draft/non-ratified target. Registered hashes match. |
| N2 - stale Channel execution metadata/ledger preface | **closed** | Channel metadata records both harnesses as executed while retaining non-ratification; CH-R11 is `realisation-executed`, and the forward-scenario preface distinguishes delivered evidence from remaining targets. The neutral gate passed. |
| N3 - current policy directed new decisions to Architecture 0.7 | **closed** | `docs/current/policies/public-boundaries.md` directs new decisions to registry-current Architecture 0.8 without implying ratification, and the neutral gate guards the wording. |
| R1 - protocol rejection fabricated zero after attribution loss | **closed** | `PortableBindingHost` records seam entry and passes `null` for the complete attribution-destroying protocol category set. PB-43's permanent production-path assertion passes, PB-46 preserves the pre-provider zero boundary, and PB-50 preserves process-loss unknown. |

No new in-scope Reference implementation, contract, architecture-target, limitation, or
documentation finding was identified.

## Commands and results

All commands ran from the isolated detached clone unless stated otherwise.

| Command | Result |
| --- | --- |
| `git clone --no-hardlinks --no-checkout <repository> <system-temp>; git checkout --detach 5150d6d774d683a6ce8e769f7472724d40f0baba; git rev-parse HEAD; git status --short` | Passed; `HEAD` exactly matched the pin and the snapshot was clean. A command-scoped `safe.directory` exception was required for the sandbox reviewer identity. |
| Registry SHA-256 comparison for the selected architecture and Reference requirements, matrix, README, and milestone ledger | Passed; all five hashes matched their registry entries. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly` | First sandboxed run passed all neutral data checks, then failed with `NU1301` because network access to NuGet metadata was denied. The approved rerun passed: 9 schemas, 84 vectors, C1-C10, all 24 Channel vectors, and 6 re-derived golden encodings; neutral-provider build reported 0 warnings/0 errors; dependency inspection found 2 resolved libraries and no Brontide assembly. |
| `dotnet test .\Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj -nologo --filter "FullyQualifiedName~.Portable."` | Passed 189, failed 0, skipped 44. The skips were the expected environment-gated provider/cross-process cases, which were run separately below. |
| Build the Reference and neutral provider executables, set `BRONTIDE_REFERENCE_PROVIDER` and `BRONTIDE_NEUTRAL_PROVIDER`, then run `PortableCrossProcessTests|PortableNeutralProviderTests` with `--no-build --no-restore` | Both builds passed with 0 warnings/0 errors. Focused process evidence passed 29, failed 0, skipped 0. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1` | Passed; 21 project references checked and Reference dependency direction was valid. |
| Focused `dotnet test` filter for PB-43 `An_Outcome_claiming...`, PB-46 `An_Operation_outside...`, and PB-50 `A_peer_that_ends...` | Passed 3, failed 0, skipped 0. The assertions establish unknown, known zero, and unknown respectively. |
| Read-only inventory of every `effectCountNotAsserted` vector plus targeted production/test inspection | Completed: exactly 10 neutral unknown-effect vectors were found. `requestEnteredSeam`, `MakesProviderEffectUnattributable`, `Rejected`, and `Lost` cover every reachable Reference host class while preserving pre-provider zero. |
| Targeted reads of AGENTS.md, the complete status-selected Architecture 0.8, Reference target/status/limitations and evidence ledger, PB8 review policy, C1-C10 matrix and properties, neutral schemas/vectors, Channel metadata, retained finding history, and the pinned correction diff | Completed. No unresolved or new Reference-scope finding was found. |

The environment selected .NET SDK `10.0.400-preview.0.26322.102`; NETSDK1057 was informational.

## Overall verdict

**conforms**

The pinned Reference realization satisfies C1-C10 within the explicitly experimental Portable
Binding 0.1 boundary, with C6 carrying its approved narrowed disposition. B1-B4, N1-N3, and R1 are
closed. The full effect-attribution defect class is closed in reachable Reference host paths: every
neutral unknown-effect case remains unknown after the seam, and pre-provider known-zero cases remain
zero. PB8 Step 5 may close for the Reference scope at this pin.
