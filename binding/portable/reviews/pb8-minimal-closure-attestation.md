# PB8 Minimal Portable Binding closure attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-minimal-closure-review-2026-08-11`
- **Review date:** 2026-08-11
- **Pinned commit:** `5150d6d774d683a6ce8e769f7472724d40f0baba`
- **Stack:** Brontide Minimal Stack only
- **PB8 step:** Step 5 closure review
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Implementation actor:** no
- **Isolation:** fresh local clone created with `git clone --no-hardlinks --no-local`, checked out
  detached at the complete pin. The clone was clean before inspection and after the temporary
  defect-class probe. The shared mutable worktree was not review evidence.
- **Retained review input:** the prior PB8 attestations were read only as finding history. No
  implementation-session private reasoning was available.

This review covers the Minimal realization, its neutral contract, and the closure of the retained
Minimal effect-attribution findings. It does not attest Reference implementation correctness,
ratify Architecture 0.8 or Channel, or promote Portable Binding beyond experimental version 0.1.

## Architecture, local target, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, exactly matching the registry.
The registry records no latest ratified architecture. The registered hashes for the shared 0.8
requirements and Minimal's 0.8 matrix, README, and milestone ledger also match their files.

The selected architecture keeps Composition, Components, Binding Plans, Portable Binding, Channel,
transport, provider selection, and lifecycle machinery outside Brontide Base. It requires exact
Shape identity/version and declared projection, strict authority positions, local and fail-closed
Capability evaluation, explicit operational observations, implementation independence, and honest
separation between experimental evidence and ratification.

`Minimal/README.md` states **Designed for: Brontide Architecture 0.8**, Complete Draft, not
ratified, and **Partial implementation with explicitly labelled experiments**. The material
Portable Binding 0.1 limitations remain:

- copied immutable blobs and addressing-only handles are the exercised resource floor; borrowing,
  transfer, lifetime/reuse, release signalling, and fallback are not implemented capabilities;
- retry, cancellation, streaming, ordering, and exactly-once execution are non-promises;
- the process realization assumes an already-connected duplex and a trusted local account,
  launcher, and selected executable; it claims no cryptographic peer identity, hostile-provider
  protection, or multi-tenant isolation;
- PB7 accepts one already-resolved direct, distinct `1..1` position; discovery, acquisition,
  provider selection, Provider Sets, mediation, generations, hot swap, and Relational
  Initialisation remain outside Portable Binding 0.1; and
- the evidence is experimental and establishes neither a stable public version nor Channel
  ratification.

## C1-C10 decisions

Exactly one PB8 verdict is recorded for each capability.

| Capability | Verdict | Rationale and concrete evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact ordered negotiation precedes effects, unknown declarations fail closed, compact ids remain binding-scoped, and the answering provider is verified. Neutral validation and all enabled Minimal portable realizations passed. |
| C2 | **conforms** | Negotiation or PB7 preflight produces one immutable, inspectable plan or no plan; provider facts come from the offered document. Native, process, foreign-provider, and neutral-provider evidence passed. |
| C3 | **conforms** | Authority remains local, no Capability crosses the trust seam, and local denial is frameless and effect-free. The host's pre-emission decision and opening paths retain an explicit known zero. |
| C4 | **conforms** | The envelope, correlation, result, protocol/process-category, and observer-relative failure-domain distinctions pass. The correction classifies post-request correlation mismatch, malformed message, unsupported kind, state violation, and internal protocol failure as unattributable, and every process failure uses the unknown form. PB-43, PB-46, and PB-50 passed with `None`, `Some 0L`, and `None` respectively. |
| C5 | **conforms** | The Shape/reference floor, deterministic CBOR, required fields and Fragments, additive payload projection, strict authority/control positions, and adversarial decoder evidence passed. |
| C6 | **approved-disposition** | Inline values, copied immutable blobs, and addressing-only handles are implemented with scope and integrity enforcement. Decision 9's narrower floor explicitly leaves borrowing, transfer, lifetime/reuse, release signalling, and fallback unexercised and makes no broader claim. |
| C7 | **conforms** | Minimal owns its F# realization and imports no Reference runtime or private types. Direct/process parity, same-stack process, Reference-provider, neutral-provider, and the 23-project boundary guard passed. |
| C8 | **conforms** | Bounds and lifecycle transitions are explicit; replay, limits, withdrawal, interruption, and termination remain fail closed. Post-request state violation and every process-failure category preserve unknown effect attribution where the neutral contract does not permit a count. |
| C9 | **conforms** | `ProviderEffectCount` is an option, success requires a known count, and terminal failure observations now preserve the observer's knowledge boundary. The checked-in PB-43 and PB-50 paths assert `None`; PB-46 retains `Some 0L`; an isolated all-category probe confirmed every currently reachable unattributable protocol/process path uses `None`. |
| C10 | **conforms** | The neutral provider built with two resolved libraries and no Brontide assembly. Minimal passed against its own provider, the Reference provider, and the implementation-neutral provider with no skips in the enabled portable and cross-process runs. |

## Effect-attribution defect-class review

The neutral corpus contains ten `effectCountNotAsserted` vectors. Every member reachable through
the Minimal host is covered by one of two production paths:

| Neutral scenario | Minimal production classification | Result |
| --- | --- | --- |
| PB-40 timeout, PB-41 interrupted frame, PB-50 peer terminated, and the PB-51/PB-52 process-category/domain families | Every `Interrupted ProcessFailure` reaches `ObservationBuilder.unknownEffect`, independent of its one selected process category or failure domain. | `None` |
| PB-43 correlation mismatch, PB-44 missing correlation identity, PB-45 unsupported envelope kind, PB-49 internal protocol failure, and PB-84 late Outcome/state violation | After `conversation.Request` has entered the seam, `effectCountAfterRequest` maps `CorrelationMismatch`, `MalformedMessage`, `UnsupportedKind`, `InternalProtocolFailure`, and `StateViolation` to the unknown builder. | `None` |
| PB-46 unsupported Operation and other host-owned preparation/opening refusals proven before emission/provider contact | The two pre-request `rejected` call sites pass an explicit known zero; PB-46 reaches that path and asserts it. | `Some 0L` |

This is phase-sensitive rather than category-only: a pre-request state violation remains known zero,
while the same category observed after an emitted request is unknown. A temporary test added only to
the isolated clone supplied each of the five unattributable protocol categories after a request and
each of the seven process categories through the production `PortableBindingHost`. All twelve
observations were `None`. The probe was removed, `git diff --check` and `git status --short` were
clean, and `HEAD` still matched the pin.

## Finding dispositions

| Finding | Disposition at `5150d6d` | Evidence |
| --- | --- | --- |
| B1 - contradictory provider identity and plan-source rules | **closed** | The component contract, Binding Plan, composition handoff, Decision 11, PB-78, and PB-83 verify the answering provider and source plan facts from the offered document. Enabled substitution evidence passed. |
| B2 - impossible post-withdrawal Outcome acceptance | **closed** | The neutral lifecycle has no withdrawn-to-Outcome transition; PB-84 requires state violation, and Minimal's host classifies a late post-request Outcome without fabricating effect knowledge. |
| B3 - mandatory/fabricated effect count | **closed for Minimal** | The neutral optional representation is implemented by `int64 option`. Process failures use `unknownEffect`; post-request protocol categories that destroy attribution use `None`; verified pre-provider refusals retain `Some 0L`. The full current class probe and PB-43/PB-46/PB-50 passed. |
| B4 - absent capability-wide properties | **closed** | The neutral gate validates exactly one falsifiable `all-vectors-with-capability` property for each C1-C10 and all 84 vectors. C9-P1 is now true on the corrected Minimal production paths inspected and executed here. |
| N1 - stale Architecture 0.7 target text | **closed** | Registry, selected architecture, Minimal README, Portable Binding matrix/README/plan, public-boundary policy, completeness record, and neutral-provider boundary consistently state the local Architecture 0.8 target without implying ratification. |
| N2 - stale Channel execution metadata/ledger text | **closed** | Both stack harnesses remain recorded as executed, CH-R11 remains `realisation-executed`, and delivered Portable Binding evidence is distinguished from remaining targets and ratification. The neutral gate passed. |
| N3 - current policy directed new decisions to Architecture 0.7 | **closed** | The current public-boundary policy directs new decisions to current Architecture 0.8 while preserving its Complete Draft, non-ratified status. The neutral gate passed its regression check. |
| Retained Minimal R1 - process loss fabricated zero | **closed** | `interrupted` uses `ObservationBuilder.unknownEffect`; PB-50 reaches the real peer-termination path and asserts `None`. |
| Final Minimal F1 - post-request correlation refusal fabricated zero | **closed** | `effectCountAfterRequest` sends correlation mismatch to the unknown builder, and the checked-in PB-43 real-seam test now asserts `None`. The focused test and the broader isolated class probe passed. |

No unresolved or new in-scope Minimal finding was identified.

## Commands and results

Commands ran from the isolated detached clone unless stated otherwise.

| Command | Result |
| --- | --- |
| `git clone --no-hardlinks --no-local <repository> <isolated-path>; git checkout --detach 5150d6d774d683a6ce8e769f7472724d40f0baba; git rev-parse HEAD` | Passed; `HEAD` exactly matched the full pin and the initial clone was clean. |
| SHA-256 comparison for the selected architecture and registered Minimal requirements, matrix, README, and milestone ledger | Passed; all five actual hashes matched the registry. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly` | First sandboxed run completed all neutral structural checks, then NuGet signature metadata access failed with `NU1301`. Approved rerun passed: 9 schemas, 84 vectors over C1-C10 and all 24 Channel vectors, 6 re-derived golden encodings, neutral-provider build with 0 warnings/errors, and 2 resolved libraries with no Brontide assembly. |
| Build Minimal provider, Reference provider, and Minimal interchange tests | Passed; all three builds completed with 0 warnings and 0 errors. Reference was built only as the foreign endpoint for the Minimal-host process evidence. |
| Set `BRONTIDE_REFERENCE_PROVIDER`, `BRONTIDE_MINIMAL_PROVIDER`, and `BRONTIDE_NEUTRAL_PROVIDER`, then `dotnet test ... --no-build --no-restore --filter 'FullyQualifiedName~Brontide.Minimal.Interchange.Tests.Portable'` | Passed **222**, failed 0, skipped 0. |
| Same environment, `dotnet test ... --filter 'Category=CrossProcess&FullyQualifiedName~Portable'` | Passed **45**, failed 0, skipped 0. |
| Focused `dotnet test` filter selecting PB-43, PB-46, and PB-50 with normal console logging | Passed 3/3. PB-43 asserted `None`, PB-46 asserted `Some 0L`, and PB-50 asserted `None`. |
| Temporary isolated test over all five post-request unattributable protocol categories and all seven process categories, then `dotnet test ... --filter 'FullyQualifiedName~PB8'` | Passed 2/2, including the new 12-case production-host probe and the existing B3 unknown-form test. The temporary test was removed; the clone returned clean at the exact pin. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1` | Passed for 23 F# projects. |
| Targeted source and contract inspection of the status-selected Architecture 0.8, Minimal target/limitations, PB8 policy, retained finding history, all neutral `effectCountNotAsserted` vectors, Minimal host/conversation/observation classification, PB-43/PB-46/PB-50, parity/coverage tests, and the correction diff | Completed. |

The environment selected .NET SDK `10.0.400-preview.0.26322.102`; NETSDK1057 was informational.

## Overall verdict

**conforms**

At pinned commit `5150d6d774d683a6ce8e769f7472724d40f0baba`, C1-C5 and C7-C10 conform and C6 has its explicit
approved disposition. B1-B4, N1-N3, retained Minimal R1, and the final post-request correlation
finding are closed. The standard neutral, Minimal native, enabled process, focused regression, and
boundary gates pass. The complete current Minimal effect-attribution class preserves `None` where
the neutral contract says the effect is unknowable and retains `Some 0L` where pre-provider refusal
proves zero. PB8 Minimal Step 5 closure therefore conforms within the declared experimental 0.1
boundary.
