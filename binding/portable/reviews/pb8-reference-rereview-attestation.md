# PB8 Reference Portable Binding re-review attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-reference-rereview-2026-08-11`
- **Review date:** 2026-08-11
- **Pinned commit:** `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`
- **Stack:** Reference only
- **PB8 step:** Step 5 fresh-context re-review
- **freshContext:** `true`
- **implementationContextAccess:** `none`
- **Implementation actor:** no
- **Isolation:** fresh local clone under the system temporary directory, checked out detached at the
  pinned commit. The snapshot was clean before review. A single temporary assertion was later added
  only in that isolated clone to execute the defect hypothesis recorded below; no implementation
  change was made in the shared worktree.
- **Retained review input:** only the initial Reference and neutral PB8 attestations were read, and
  only to identify findings whose disposition had to be verified. The Minimal attestation and prior
  implementation-session reasoning were not read.

This record reviews the Reference realization at the new pin. It does not review Minimal correctness,
does not repair findings, and does not promote Portable Binding beyond experimental 0.1 evidence.

## Architecture and Reference target

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, matching the registry. The
registry records no latest ratified architecture (`revision` and `path` are null; status is `none`).

The selected architecture keeps Composition, Component, Binding Plan, Portable Binding, Channel,
transport, provider selection, and lifecycle machinery outside Brontide Base. It requires behavioural
conformance, implementation independence, exact Shape identity/version and declared projection,
strict authority positions, explicit operational observations, and fail-closed authority boundaries.
Section 18.1 records Portable Binding as a proposed first-party default Component-interchange seam;
implementation evidence does not ratify it.

`Reference/README.md` states **Designed for: Brontide Architecture 0.8**, Complete Draft, not
ratified, with status **Partial implementation with explicitly labelled experiments**. The Portable
Binding code remains in `Brontide.Reference.Experimental.Binding/Portable/`, outside Core. The local
limitations remain material:

- Portable Binding 0.1 is experimental, outside Base, and does not ratify Architecture 0.8, Channel,
  Composition, or a stable public package/wire contract.
- C6 exercises copied immutable blobs and addressing-only handles, but not borrowing, transferred
  ownership, lifetime expiry, release signalling, or fallback.
- The PB7 handoff accepts only an already-resolved distinct `1..1` position; discovery, acquisition,
  wider Provider Sets, mediation, generations, provider-selection policy, and hot swap remain out of
  scope and are refused rather than approximated.
- Network security, federation, retries, cancellation, streaming, ordering, and exactly-once
  execution are not promised.

## C1-C10 decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and concrete evidence |
| --- | --- | --- |
| C1 | **conforms** | `PortableNegotiation.RequireProvider` now compares the required and offered provider before compact-id assignment or plan creation, refusing a mismatch as `unsupported-contract/provider-mismatch`. The resulting plan receives `offered.Provider`; PB-83 and the named establishment tests pin both the refusal and the answering-provider fact. |
| C2 | **conforms** | The immutable plan reads `Provider`, `SelectedProvider`, and `AnsweringProvider` from the offered document. The corrected composition schema and PB-78 no longer preserve the superseded required-document rule; preflight and negotiation both fail closed before a partial plan survives. Native plan and handoff tests passed. |
| C3 | **conforms** | Authority evaluation remains local, cross-trust Capability content is rejected before emission, denial remains frameless and effect-free, and the Reference suites exercise false/unknown constraints, missing declarations, and forbidden Capability-shaped content. |
| C4 | **conforms** | The envelope, correlation, four-way failure separation, protocol categories, process categories, and observer-relative failure domains remain explicit. The neutral gate covered all 24 Channel vectors and the Reference suite passed the native category and process-failure evidence. |
| C5 | **conforms** | The Reference Shape catalogue and codec preserve the declared scalar/composite floor, exact required fields and Fragments, additive payload projection, and strict authority/control positions. Six deterministic encodings re-derived successfully. |
| C6 | **approved-disposition** | The declared 0.1 resource floor is implemented and tested for representation, scope, access, copied/provider-retained ownership, integrity, refusal observations, and copy accounting. Decision 9's explicit non-exercise of borrow interval, lifetime/reuse, release signalling, fallback, and transferred ownership remains an approved narrowed scope, not evidence for those future behaviours. |
| C7 | **conforms** | Direct and negotiated-process Reference realizations continue to compare the declared parity profile across the existing scenarios. The native portable suite passed, and 29 focused Reference-process/neutral-provider tests passed with no skips. Reference dependency direction passed, and the neutral provider resolved only two non-Brontide libraries. |
| C8 | **conforms** | `PortableLifecycle` has no `withdrawn -> outcome` transition. PB-84 and the new named native test pin a late Outcome as `state-violation`, consistent with the corrected lifecycle text. Bounds, replay, establishment, readiness, withdrawal, termination, and illegal-state evidence passed. |
| C9 | **does-not-conform** | The schema and public type now declare an explicit unknown effect count, but the production loss path does not use it. `PortableBindingHost.Lost` passes literal `0` to `PortableObservationBuilder.Build` for every process failure. A real peer-termination path after the request crossed the seam therefore reports zero despite being unable to know whether the provider performed an effect. A temporary assertion against that existing path failed with `Expected: null; But was: 0`. The new green test only clones a successful observation with `ProviderEffectCount = null`; it does not exercise the host's failure path. This reproduces the substance of retained B3 and falsifies C9-P1's own counterexample. |
| C10 | **conforms** | The neutral gate validated 84 unique vectors over C1-C10 and built the implementation-neutral provider; its dependency inspection found two resolved libraries and no Brontide assembly. The Reference portable suite passed 189 native tests, and the focused Reference-process plus neutral-provider run passed 29/29 without skips. The reciprocal Minimal-provider direction remains outside this Reference-only re-review. |

## Retained finding dispositions

| Finding | Disposition at `c6f9d51` | Evidence |
| --- | --- | --- |
| Reference F1 / neutral N1 — stale Architecture 0.7 target text | **closed** | The selected architecture introduction, contract matrix, PB8 Step 5, public-boundary policy, and neutral-provider README now consistently say both stacks locally target Architecture 0.8 while preserving non-ratification and experimental status. |
| B1 — contradictory provider identity and plan-source rules | **closed** | `component-contract.json`, `binding-plan.json`, `composition-handoff.json`, PB-78, PB-83, `PortableNegotiation`, and `PortableBindingPlan` now agree: negotiation refuses a provider mismatch and plan provider facts name the offered/answering provider. Native tests passed. |
| B2 — impossible post-withdrawal Outcome acceptance | **closed** | The contract now defines a late Outcome as illegal after withdrawal, PB-84 fixes the expected result, the Reference state machine has no such transition, and its named regression test passed in the unmodified pinned suite. |
| B3 — mandatory fabricated zero effect count | **unresolved; blocking C9 and overall conformance** | The schema and nullable API repair the representation, but `PortableBindingHost.Lost` still fabricates zero on timeout, interruption, peer loss, and other process failures. The peer-termination test probe failed exactly for this reason. |
| B4 — no capability-wide properties | **declaration gap closed; conformance not established for C9** | `capability-properties.json` now declares exactly one `all-vectors-with-capability` property with a concrete counterexample for every C1-C10, and the neutral gate enforces that shape. However C9-P1 is demonstrably false in the Reference loss path because its stated counterexample—peer loss reported as zero without evidence—occurs. The repository also contains no native test named for C1-P1 through C10-P1; the gate validates property presence and form, not each statement's runtime truth. |
| N2 — stale Channel execution metadata | **partly closed; documentation-closure finding remains** | `conformance/channel-0.1-vectors.json` now says both stack harnesses executed the vectors, and CH-R11 is `realisation-executed`. The forward-scenario introduction still says “none is implemented here” immediately before two scenarios marked Delivered and one Partly delivered. This is nonblocking for C1-C10 semantics but remains blocking for PB8 documentation closure under the retained finding's own disposition. |

## Findings

### R1 — blocking — Reference process loss still fabricates zero provider effects (C9)

`PortableObservation.ProviderEffectCount` is nullable and `RequireComplete` accepts null on failure,
but the normal construction API and host loss path did not migrate with that public change:

- `PortableObservationBuilder.Build` still requires `long providerEffectCount`;
- `PortableBindingHost.Lost` always passes `0`; and
- the new `An_unobservable_provider_effect_uses_the_declared_unknown_form` test constructs an
  observation with `with { ProviderEffectCount = null }` after a successful interaction, bypassing
  the production failure path.

The existing peer-termination scenario is a nameable counterexample. The host sends a request, the
peer output closes before an Outcome, and the host cannot determine whether an effect occurred. The
temporary isolated-clone assertion that the observation use `unknown` failed because the actual value
was zero. This is the same knowledge-fabrication class B3 identified, not merely missing test coverage.

### R2 — PB8 documentation closure — the Channel forward-scenario preface remains stale

The Channel vector status and CH-R11 disposition are corrected, but
`docs/future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md` still introduces its
forward scenarios by saying none is implemented there and immediately labels cross-stack vectors and
Portable Binding delivered. The retained N2 contradiction therefore remains in one of its two named
locations.

No other blocking Reference implementation finding was identified.

## Commands and results

All review commands ran from the isolated detached clone unless the row says otherwise.

| Command | Result |
| --- | --- |
| `git clone --no-hardlinks --no-checkout <repository> <system-temp>; git checkout --detach c6f9d51; git rev-parse HEAD` | Passed; HEAD was exactly `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`. A command-scoped `safe.directory` exception for the source repository was required by the sandbox identity. |
| `Get-FileHash -Algorithm SHA256 docs/current/architecture/Brontide-Architecture-0.8.md` | Passed; `CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, exactly matching the status registry. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly` | First run: data validation passed, then restore failed with `NU1301` because sandbox network access was denied. Approved rerun passed completely: 9 schemas, 84 vectors, C1-C10, all 24 Channel vectors, 6 re-derived golden encodings; neutral provider built with 0 warnings/0 errors; 2 resolved libraries and no Brontide assembly. |
| `dotnet test .\Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj -nologo --filter "FullyQualifiedName~.Portable."` | Passed: 189, failed 0, skipped 44. The skips were environment-gated real-process, cross-stack, and neutral-provider cases. |
| Build `Brontide.Reference.Interchange.Provider`, set `BRONTIDE_REFERENCE_PROVIDER` and `BRONTIDE_NEUTRAL_PROVIDER`, then run the focused cross-process/neutral-provider filter with `--no-build --no-restore` | Passed: 29, failed 0, skipped 0. Provider build reported 0 warnings and 0 errors. |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1` | Passed: 21 project references checked; Reference dependency direction valid. |
| Add only in the isolated clone an assertion that the existing peer-termination observation has unknown provider effect count, then run that one test | Failed as predicted: expected null, actual `0`. This confirms R1 is in the production host-loss path. The assertion is not part of this attestation commit or the shared worktree. |
| Targeted reads/searches of the status registry, selected architecture, Reference README and limitations, PB8 review policy, initial Reference and neutral findings, C1-C10 plan and matrix, all neutral schemas/vectors relevant to the findings, Reference Portable implementation, coverage/parity/lifecycle/observation tests, and correction diff | Completed. The Minimal attestation and implementation-session reasoning were not accessed. |

The environment selected .NET SDK `10.0.400-preview.0.26322.102`; NETSDK1057 was informational.

## Overall verdict

**does-not-conform**

The pinned Reference realization still violates C9 because its process-failure observation fabricates
a zero provider-effect count when the effect is unknowable. B1 and B2 are closed, the B4 property
declarations now exist, and F1/N1 is closed; B3 remains substantively unresolved, C9-P1 is false in
the Reference runtime, and N2 remains partly unresolved for PB8 documentation closure. PB8 Step 5
cannot close on this Reference re-review.
