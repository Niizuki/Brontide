# PB8 Minimal Portable Binding final attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-minimal-final-review-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `fe299c71e2e77199ccdedfba552e193d3e0f91df`
- **Snapshot:** fresh isolated clone, checked out detached at the exact pinned commit; the clone was
  restored clean after a temporary diagnostic assertion and `HEAD` remained the pinned hash
- **Scope:** final PB8 Step 5 review of the Minimal realization, including closure of the retained
  Minimal finding and the Minimal consequences of neutral findings B1-B4 and N1-N3
- **Excluded:** Reference implementation correctness and reliance on implementation-session
  reasoning

I am not an implementation actor. I read the repository instructions, status registry, selected
Architecture 0.8 material relevant to Portable Binding and conformance, the Minimal local target and
limitations, the PB8 review policy, the retained initial and first re-review attestations only as
findings to reproduce or close, the full neutral contract corpus, the Channel ledger and vectors,
and the relevant Minimal production paths and tests. The shared mutable worktree was not used as
review evidence.

## Architecture, local target, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. Its registered SHA-256
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579` matches the selected document.
The registry records **no latest ratified architecture**. The registered hashes for Minimal's
Architecture 0.8 matrix, README, milestone ledger, and the shared requirements also match.

Architecture 0.8 says both stacks currently target 0.8 while their local evidence bounds that
claim. Sections 16 and 18.1 require portable Shape contracts and make Composition, Binding Plans,
and Portable Binding work-in-progress outside Base. Section 19 keeps Channel provisional, and the
open questions retain the exact binding surface as unsettled architecture work. This attestation
therefore neither ratifies Architecture 0.8 nor promotes Portable Binding, Channel, or Composition.

`Minimal/README.md` states **Designed for Architecture 0.8, Complete Draft, not ratified** and
**Partial implementation with explicitly labelled experiments**. The material 0.1 limits remain:

- copied immutable blobs and addressing-only handles are the exercised resource floor; borrowing,
  transfer, lifetime/reuse, release signalling, and fallback are not implemented capabilities;
- retry, cancellation, streaming, ordering, and exactly-once execution are non-promises;
- the process realization assumes an already-connected duplex and a trusted local account,
  launcher, and selected executable; it provides no cryptographic peer identity, hostile-provider
  protection, or multi-tenant isolation;
- the PB7 handoff accepts one already-resolved direct, distinct `1..1` position; discovery,
  acquisition, provider selection, Provider Sets, mediation, generations, hot swap, and relational
  initialization remain outside Portable Binding 0.1; and
- all evidence is experimental and establishes neither a stable public version nor Channel
  ratification.

## C1-C10 decisions

Exactly one PB8 verdict is recorded for each capability.

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact ordered negotiation precedes effects, unknown declarations fail closed, compact ids are binding-scoped, and the answering provider is verified. Neutral validation and all enabled Minimal realizations passed. |
| C2 | **conforms** | Negotiation or PB7 preflight produces exactly one immutable plan or no plan; provider facts come from the offered document and remain inspectable. Native, process, foreign-provider, and neutral-provider evidence passed. |
| C3 | **conforms** | Authority stays local, Capability-shaped content is refused before crossing a trust boundary, and local denial is frameless with a known zero effect count. The relevant negative and parity evidence passed. |
| C4 | **does-not-conform** | PB-50 now correctly reports peer termination with an unknown effect count, and the result/category taxonomy otherwise passes. PB-43 nevertheless receives an Outcome carrying a different request identity after the peer reports one effect, then `PortableBindingHost.rejected` emits `Some 0L`. The neutral PB-43 expectation requires `effectCountNotAsserted`; the host cannot attribute the foreign Outcome to its outstanding request. Finding F1 is blocking. |
| C5 | **conforms** | The Shape/reference floor, deterministic CBOR, strict authority/control positions, additive payload projection, and adversarial decoder evidence passed. |
| C6 | **approved-disposition** | Inline values, copied immutable blobs, and addressing-only handles are implemented with scope and integrity enforcement. The narrower 0.1 floor explicitly leaves borrowing, transfer, lifetime/reuse, release signalling, and fallback unimplemented and makes no broader claim. |
| C7 | **conforms** | Minimal owns its F# realization and imports no Reference runtime or private types. Direct/process parity, same-stack process, Reference-provider, neutral-provider, and the 23-project boundary guard passed. |
| C8 | **conforms** | Bounds and lifecycle transitions are explicit; late Outcome after withdrawal is refused, and PB-50 now preserves unknown effect attribution after peer loss. Replay, limit, interruption, and release-gate evidence passed. |
| C9 | **does-not-conform** | The observation type and `ObservationBuilder.unknownEffect` can represent unobservable effects, success requires a known count, and PB-50 reaches that production path. However C9's schema governs every terminal observation, and PB-43 still rewrites an unobservable post-request correlation failure as known zero through `rejected`. The executable probe observed `Some 0L`, violating the schema's prohibition on fabricated zero. Finding F1 is blocking. |
| C10 | **conforms** | Minimal passed against its own provider, the Reference provider, and the implementation-neutral provider with no skips in the enabled portable run. The neutral provider resolved two libraries and no Brontide assembly; the Minimal boundary guard passed. |

## Retained finding dispositions

| Finding | Disposition | Evidence |
| --- | --- | --- |
| Initial Minimal F1 / neutral N1 - stale Architecture 0.7 target language | **closed** | Registry, selected architecture, Minimal README, Portable Binding matrix/README/plan, current public-boundary policy, completeness record, and neutral-provider boundary consistently state the Architecture 0.8 local target without implying ratification. The neutral gate's N1 check passed. |
| Neutral B1 - provider identity contradiction | **closed** | Component contract, Binding Plan, composition handoff, Decision 11, PB-78, and PB-83 consistently verify the answering provider and read provider facts from the offered document. Enabled substitution tests passed. |
| Neutral B2 - post-withdrawal Outcome contradiction | **closed** | The neutral lifecycle permits Outcome only in `active`, has no withdrawn-Outcome transition, and PB-84 expects `state-violation`. Minimal reaches and passes that transition. |
| Neutral B3 - fabricated effect count | **neutral declaration closed; Minimal realization still does not conform** | The schema uses required `optional<Integer.Signed64>` with `unknown`, properties/vectors state when attribution is impossible, and the former peer-loss defect is closed by `interrupted -> ObservationBuilder.unknownEffect`; PB-50 passed and observed `None`. The same semantic class remains reachable through PB-43's post-request correlation refusal, which still observes `Some 0L` (F1). |
| Neutral B4 - missing capability-wide properties | **closed** | `capability-properties.json` contains exactly one falsifiable `all-vectors-with-capability` property for each C1-C10, and the neutral gate enforces their presence and form. |
| Neutral N2 - stale Channel execution metadata | **closed** | The shared Channel contract says both stack harnesses executed the vectors; CH-R11 is `realisation-executed`; the forward-scenario introduction distinguishes Delivered entries from remaining targets and preserves non-ratification. The gate's N2 checks passed. |
| Neutral N3 - current policy points new decisions to Architecture 0.7 | **closed** | `docs/current/policies/public-boundaries.md` now points new architectural decisions to current Architecture 0.8 and explicitly preserves Complete-Draft non-ratification. The gate's N3 check passed. |
| Minimal re-review R1 - process loss fabricates zero | **closed as reported** | `PortableBindingHost.interrupted` now calls `ObservationBuilder.unknownEffect`; the builder stores `None`; PB-50 asserts the real peer-termination path and passed. F1 below is a distinct post-request protocol-refusal path. |

## New finding

### F1 - blocking - post-request correlation refusal fabricates a known zero effect count (C4, C9, B3 class)

PB-43's misbehaving peer returns a successful Outcome with a request identity the host never sent
and reports `ProviderEffectCount = 1L`. `PortableProcessConversation.Request` correctly refuses the
correlation mismatch. `PortableBindingHost.Invoke` then routes that `Refused` result through the
general `rejected` helper, which calls `ObservationBuilder.build` with `0L`; `build` stores
`Some 0L`. The neutral PB-43 vector instead requires `effectCountNotAsserted` because the received
Outcome cannot be attributed to the outstanding request.

This is not merely an inferred path. In the isolated clone I temporarily strengthened the existing
PB-43 test with `ProviderEffectCount = None`. The test failed with **expected `null`; actual
`Some(0)`**. I then removed the diagnostic assertion and verified the clone had no tracked diff and
still pointed exactly to the pinned commit. The checked-in PB-43 test asserts category and failure
domain but does not assert the vector's effect-attribution expectation, so the ordinary green suite
does not detect this contradiction.

The correction should distinguish refusals known to occur before provider contact from protocol or
validation failures observed after a request was emitted. The latter need the declared unknown form
unless the observation has attributable effect evidence. This finding blocks PB8 Minimal closure.

## Commands and results

Commands ran from the isolated detached snapshot unless stated otherwise.

1. `git clone --no-local <local-repository> <isolated-path>; git checkout --detach fe299c7`
   - **Pass.** `HEAD` was exactly `fe299c71e2e77199ccdedfba552e193d3e0f91df`; the initial and final
     tracked status were clean.
2. SHA-256 checks for the selected architecture and registered Minimal current-delivery files
   - **Pass.** Architecture, Minimal matrix, Minimal README, Minimal milestone ledger, and shared
     Architecture 0.8 requirements matched the status registry.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First sandboxed attempt: all neutral data validation passed, then NuGet restore was denied by
     network policy (`NU1301`).
   - Approved rerun: **Pass.** 9 schemas, 84 vectors covering C1-C10 and all 24 Channel vectors, 6
     re-derived golden encodings, neutral-provider build with 0 warnings/errors, and 2 resolved
     libraries with no Brontide assembly.
4. Build the Reference provider endpoint, Minimal provider endpoint, and Minimal interchange tests
   - Provider builds: **Pass**, 0 warnings and 0 errors.
   - The first sandboxed test-project restore failed only on NuGet signature-network access; the
     approved rerun built successfully with 0 warnings and 0 errors.
5. `dotnet test .\Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj -nologo --no-build --no-restore`
   - **Pass.** 186 passed, 0 failed, 52 provider-path-gated skips, 238 total.
6. Set `BRONTIDE_REFERENCE_PROVIDER`, `BRONTIDE_MINIMAL_PROVIDER`, and
   `BRONTIDE_NEUTRAL_PROVIDER` to the pinned clone's built endpoints, then run the Minimal portable
   filters
   - `FullyQualifiedName~Brontide.Minimal.Interchange.Tests.Portable`: **222 passed, 0 failed, 0
     skipped**.
   - `Category=CrossProcess&FullyQualifiedName~Portable`: **45 passed, 0 failed, 0 skipped**.
7. `dotnet test ... --filter 'Name~PB-50' --logger 'console;verbosity=normal'`
   - **Pass.** The one selected PB-50 peer-termination test passed.
8. Temporary diagnostic assertion in the isolated clone followed by
   `dotnet test ... --filter 'Name~PB-43'`
   - **Expected red reproduced F1.** One selected test failed: expected `None`, actual `Some(0)`.
     The assertion was removed immediately; `git diff --check` and `git status --short` were clean.
9. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1`
   - **Pass.** Boundary verification passed for 23 F# projects.

## Overall verdict

**does-not-conform for PB8 Step 5 Minimal closure at the pinned commit.** C1-C3, C5, C7-C8, and
C10 conform; C6 has the declared approved disposition; C4 and C9 do not conform because F1 leaves a
post-request correlation failure claiming a known zero effect count when attribution is impossible.
The requested peer-loss correction is real and closes retained R1, and B1, B2, B4, N1-N3, and the
Channel ledger are otherwise closed. Passing gates do not override the directly reproduced
observable contradiction. This verdict neither challenges Minimal's Architecture 0.8 local target
nor expands the experimental Portable Binding claim.
