# PB8 Minimal Portable Binding re-review attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-minimal-rereview-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`
- **Snapshot:** fresh local clone in a system temporary directory, checked out detached at the pinned
  commit; `HEAD` matched the complete pinned hash and the snapshot was clean before review
- **Scope:** PB8 Step 5 re-review of the Minimal realization, including closure of the retained
  Minimal finding and the Minimal consequences of neutral findings B1-B4 and N1-N2
- **Excluded:** Reference implementation correctness, implementation changes, and reliance on prior
  implementation-session reasoning

I read the repository instructions, status registry, selected Architecture 0.8 material relevant to
Portable Binding and conformance, the Minimal local target and limitations, the PB8 review policy,
the initial Minimal and neutral attestations as retained findings to reproduce, the neutral
contracts and vectors, the Minimal implementation and tests, and the verification scripts. I did
not inspect the initial Reference attestation. I made no implementation or semantic change.

## Architecture, target, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. Its registered SHA-256
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579` matches the selected document
at the pinned commit. The registry reports **no latest ratified architecture**. The registered
Minimal current-delivery hashes for its Architecture 0.8 matrix, README, milestone ledger, and
shared requirements also match their files.

The architecture states that both stacks currently target the Complete Draft Architecture 0.8,
while local documents and executable evidence bound the claim. Section 18.1 keeps Composition,
Binding Plans, and Portable Binding as work-in-progress design direction outside Base; section 19
keeps Channel provisional and unratified; and section 33 leaves the exact Portable Binding surface
open at architecture level. Experimental evidence therefore neither ratifies Architecture 0.8 nor
promotes Portable Binding into Base.

`Minimal/README.md` states **Designed for Architecture 0.8, Complete Draft, not ratified** and
**Partial implementation with explicitly labelled experiments**. The Portable Binding realization
is Minimal-native and experimental. Its material limitations remain:

- version 0.1 exercises copied immutable blobs and addressing-only handles; borrowed regions,
  ownership transfer, lifetime/reuse, release signalling, and fallback remain outside its executed
  floor;
- retry, cancellation, streaming, ordering, and exactly-once execution are non-promises;
- it assumes an already-connected duplex and a locally selected executable under a trusted account
  and launcher; it claims no cryptographic peer identity, hostile-provider protection, or
  multi-tenant isolation;
- the PB7 handoff consumes an already-resolved direct, distinct `1..1` position; discovery,
  acquisition, selection policy, Provider Sets, mediation, generations, and hot swap remain outside
  this seam; and
- the evidence is experimental and does not establish a stable public version or Channel
  ratification.

## C1-C10 decisions

Exactly one PB8 verdict is recorded for each capability.

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **conforms** | Minimal performs exact, ordered contract negotiation before effects, compares the answering provider, fails closed on unknowns, and assigns compact ids only after success. PB-78/PB-83 and the fully enabled Minimal suite passed. |
| C2 | **conforms** | The Minimal handoff either freezes one immutable plan after successful negotiation or produces no plan. The provider fact comes from the offered document, and preflight/provider-substitution tests passed. |
| C3 | **conforms** | Authority remains local, Capabilities do not cross the seam, and denial is frameless with zero effects. Native direct, process, foreign-provider, and neutral-provider evidence passed. |
| C4 | **conforms** | Minimal preserves the declared envelope, correlation, protocol/process categories, and failure-domain separation. All 24 Channel vectors are covered and the fully enabled portable suites passed. The effect-attribution defect below is a C9 failure, not a fabricated-success or category-selection failure. |
| C5 | **conforms** | The canonical Shape/reference floor, deterministic CBOR, strict authority/control positions, additive payload projection, and adversarial decoding evidence passed. |
| C6 | **approved-disposition** | Inline values, copied immutable blobs, and addressing-only handles are implemented with scope and integrity checks. The narrower 0.1 floor explicitly leaves borrow interval, lifetime/reuse, release signalling, and fallback unexercised and does not claim broader support. |
| C7 | **conforms** | Minimal owns its F# realization and imports neither Reference runtime nor private types. Direct/process parity, cross-process execution, the cross-stack host direction, the neutral provider, and the 23-project boundary guard passed. |
| C8 | **conforms** | Bounds and lifecycle transitions are explicit. The corrected contract makes a late Outcome after withdrawal a state violation, and PB-84 exercises that Minimal transition. Replay, limit, interruption, and release-gate evidence passed. |
| C9 | **does-not-conform** | The neutral schema now has an explicit `unknown` effect-count form and Minimal's public record is `int64 option`, but the production process-failure path still converts an unobservable effect into `Some 0L`. The added B3 test constructs `None` manually and does not exercise that path. Finding R1 is blocking. |
| C10 | **conforms** | The fully enabled Minimal runs passed against its own provider, the Reference provider, and the implementation-neutral provider. The neutral provider resolved two libraries and no Brontide assembly; the Minimal boundary guard also passed. |

## Retained finding dispositions

| Finding | Disposition | Evidence |
| --- | --- | --- |
| Initial Minimal F1 / neutral N1 - stale Architecture 0.7 target language | **closed** | The status registry, selected architecture, Minimal README, Portable Binding matrix/README/plan, portable public-boundary section, completeness record, and neutral-provider boundary consistently state the local Architecture 0.8 target without implying ratification. The gate's N1 check passed. |
| Neutral B1 - Decision 11 contradicted | **closed** | `component-contract.json`, `composition-handoff.json`, PB-78, Minimal negotiation, and plan freezing now agree: provider mismatch is refused and the plan provider comes from the offered document. Native substitution tests passed. |
| Neutral B2 - post-withdrawal Outcome contradiction | **closed** | `withdrawn` now permits no Outcome, the transition table contains no such transition, PB-84 expects `state-violation`, and the Minimal lifecycle test reaches `active -> withdrawn` before refusing the late Outcome. |
| Neutral B3 - required fabricated effect count | **not closed for Minimal realization** | The neutral schema correctly changes the field to `optional<Integer.Signed64>` with absent form `unknown`. Minimal changes the record to `int64 option`, but `PortableBindingHost.interrupted` passes `0L` to `ObservationBuilder.build`, which always stores `Some providerEffectCount`. Thus timeout, interruption, and peer-loss observations still assert zero without evidence. |
| Neutral B4 - no property over all vectors for each capability | **closed** | `capability-properties.json` contains one `all-vectors-with-capability` property and a nameable counterexample for each C1-C10, and the neutral gate enforces their presence and shape. |
| Neutral N2 - stale Channel execution metadata | **closed** | The shared Channel vector status now says both stack harnesses execute the vectors through a conforming realization; the Channel ledger records CH-R11 as `realisation-executed` while preserving non-ratification. |

## Finding

### R1 - blocking - Minimal process failures still fabricate a known zero effect count (C9, B3)

`binding/portable/schemas/binding-observation.json` requires `providerEffectCount` to be `unknown`
when timeout, interruption, peer loss, malformed correlation, or another boundary prevents
attribution; absence must not be rewritten as zero. In
`Minimal/src/Brontide.Minimal.Binding/Portable/PortableBindingHost.fs`, the `interrupted` production
path builds a `TerminalStatus.ProcessFailure` observation but passes `0L` as the effect count. In
`PortableObservation.fs`, `ObservationBuilder.build` wraps every supplied count in `Some`, so the
observable result is `Some 0L` rather than `None`.

The new test named `PB8 B3 an unobservable provider effect uses the declared unknown form` does not
pin this production path: it first executes a successful interaction and then creates a record copy
with `ProviderEffectCount = None`. Consequently the green suite demonstrates that the type and
parity formatter can represent `unknown`, but not that an actual process failure produces it. This
is the same semantic class B3 identified and blocks Minimal PB8 closure.

No other unresolved in-scope Minimal finding was identified.

## Commands and results

Commands ran from the isolated detached snapshot unless stated otherwise.

1. `git clone --no-hardlinks --no-checkout <local-repository> <system-temp>; git checkout --detach c6f9d51`
   - **Pass.** `HEAD` was exactly `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`; initial status was clean.
2. SHA-256 checks for the selected architecture and registered Minimal current-delivery files
   - **Pass.** Architecture, Minimal matrix, Minimal README, Minimal milestone ledger, and shared
     Architecture 0.8 requirements all matched the status registry.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First sandboxed attempt: neutral structural validation passed, then NuGet network policy
     blocked the neutral-provider restore (`NU1301`).
   - Approved rerun: **Pass.** 9 schemas, 84 vectors covering C1-C10 and all 24 Channel vectors, 6
     re-derived golden encodings, neutral-provider build with 0 warnings/errors, and 2 resolved
     libraries with none from either stack.
4. `dotnet test .\Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj -nologo`
   - **Pass.** 186 passed, 0 failed, 52 provider-path-gated skips, 238 total.
5. Build both provider endpoints, set `BRONTIDE_REFERENCE_PROVIDER`,
   `BRONTIDE_MINIMAL_PROVIDER`, and `BRONTIDE_NEUTRAL_PROVIDER` to the pinned snapshot outputs, then
   run the Minimal portable filters
   - Provider builds: **Pass**, 0 warnings and 0 errors.
   - `FullyQualifiedName~Brontide.Minimal.Interchange.Tests.Portable`: **222 passed, 0 failed, 0
     skipped**.
   - `Category=CrossProcess&FullyQualifiedName~Portable`: **45 passed, 0 failed, 0 skipped**.
6. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1`
   - **Pass.** Boundary verification passed for 23 F# projects.
7. Read-only trace of the B3 production and test paths
   - **Finding R1 reproduced:** `PortableBindingHost.interrupted` supplies `0L` to a builder that
     stores `Some`, while the B3 test supplies `None` only through a manually copied observation.

## Overall verdict

**does-not-conform for PB8 Step 5 Minimal closure at the pinned commit.** C1-C5, C7-C8, and C10
conform; C6 has the declared approved disposition; C9 does not conform because Finding R1 leaves the
Minimal realization of neutral finding B3 unresolved. Passing gates do not override the contradicted
observable semantics, and this verdict neither challenges the Architecture 0.8 local target nor
expands the experimental Portable Binding claim.
