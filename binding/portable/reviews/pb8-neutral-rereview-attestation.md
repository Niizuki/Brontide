# PB8 neutral Portable Binding re-review attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-neutral-rereview-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`
- **Snapshot:** fresh local clone in a system temporary directory, checked out detached at the
  pinned commit; the snapshot was clean at the end of review
- **Scope:** PB8 Step 5 re-review of the implementation-neutral Portable Binding contract
- **Retained findings reviewed:** B1-B4 and N1-N2 from
  [`pb8-neutral-attestation.md`](./pb8-neutral-attestation.md)
- **Excluded:** Reference and Minimal implementation correctness, native or cross-stack execution,
  and changes to the reviewed artifacts

I am not an implementation actor and had no access to the implementation session's private
reasoning. The retained negative attestation was read only as the findings to reproduce or close.
The review independently inspected the status registry and current Architecture 0.8, both local
stack targets and limitations, the PB8 plan and review policy, Decision 11, every neutral schema and
vector file, the Channel vectors and ledger, the neutral-provider boundary, and the PB8-facing
current boundary documentation.

## Architecture and programme status

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, matching the registry. The
registry reports **no latest ratified architecture**.

Both `Reference/README.md` and `Minimal/README.md` state **Designed for Architecture 0.8, Complete
Draft, not ratified**, and describe partial implementations with explicitly labelled experiments.
Portable Binding 0.1 remains experimental work designed for Architecture 0.8 sections 16 and 18.1.
It is not Brontide Base and does not ratify Architecture 0.8, Channel, Composition, or its
deterministic-CBOR representation.

The material 0.1 limitations remain accurately bounded in the portable contract itself: copied
immutable blobs and an addressing-only handle are the resource floor; borrowing, transfer,
lifetime/reuse, release signalling, and fallback are not implemented capabilities; retry,
cancellation, streaming, ordering, and exactly-once execution are non-promises; the process seam
assumes an already-connected duplex and trusted launcher and supplies no cryptographic peer identity
or multi-tenant isolation; and the PB7 handoff accepts only an already-resolved `1..1` distinct
exposure. Decision 13 retains the fail-closed Relational Initialisation limitation for 0.1 and
schedules the wider lifecycle contract for 0.2.

## Overall verdict

**does-not-conform**

The neutral contract corrections conform: B1-B4 are closed, the capability-wide properties are
sufficient for the stated Decision 10 practice, and N1-N2 as originally reported are closed. The
NeutralOnly gate is green. PB8 documentation closure nevertheless still has one unresolved in-scope
finding, N3 below: the current public-boundary policy's governing-architecture header remains pinned
to Architecture 0.7 while the registry and both stack targets are 0.8. This does not invalidate the
C1-C10 neutral semantics, but PB8 Step 5 requires the current architecture and limitations to be
reported consistently.

## C1-C10 decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact negotiation, unknown-declaration refusal, compact-id scoping, and provider-identity matching are coherent across `component-contract.json`, `binding-plan.json`, Decision 11, PB-78, and PB-83. C1-P1 quantifies over all 15 C1 vectors and gives a concrete plan/effect counterexample. |
| C2 | **conforms** | One immutable plan is produced only after successful negotiation. Both provider facts name who answered from the offered document, and the handoff no longer preserves the superseded required-document rule. C2-P1 covers all 15 C2 vectors with partial-plan and post-freeze mutation counterexamples. |
| C3 | **conforms** | No Capability transfer, frameless local denial, strong-Kleene authority handling, and zero-effect pre-provider refusal are consistent across the schema and 10 vectors. C3-P1 is capability-wide and falsifiable by either transported authority or a denied effect. |
| C4 | **conforms** | The envelope kinds, correlation rules, 12 protocol categories, 7 process categories, 5 failure domains, and four-way failure separation cover all 24 Channel vectors. C4-P1 applies to all 16 C4 vectors and can fail on fabricated success, competing categories, or lost correlation. Channel remains unratified. |
| C5 | **conforms** | The Shape floor, additive payload projection, strict authority positions, forbidden runtime/control content, and golden encodings agree with Architecture 0.8 section 16. C5-P1 ranges over all 14 C5 vectors and names concrete malformed or authority-bearing values. |
| C6 | **approved-disposition** | The exercised 0.1 floor coherently covers representation, scope, access, ownership, and integrity. Borrow interval, lifetime/reuse, release, and fallback remain explicitly declared-but-unexercised because no lifetime-bearing flavor exists; Decision 9 assigns widening to 0.2. C6-P1 covers all 11 C6 vectors and prevents false resource acceptance, effects, or observation claims. |
| C7 | **conforms** | The parity profile explicitly separates compared from realization-dependent fields, and the four C7 vectors retain both parity and independence evidence. C7-P1 is global and falsifiable by disagreement in any compared field; PB-62's separate no-shared-runtime expectation remains independently testable rather than being substituted by the property. |
| C8 | **conforms** | The lifecycle now consistently makes a late Outcome after withdrawal illegal: `outcome` is legal only in `active`, no withdrawn Outcome transition exists, and PB-84 requires `state-violation` with unknown effect count. Limits and replay remain fail-closed. C8-P1 ranges over all 18 C8 vectors and names illegal-state, replay, and over-limit counterexamples. |
| C9 | **conforms** | `providerEffectCount` is a required optional value with explicit absent form `unknown`; known counts must be non-negative and success must have a known count. Ten vectors use `effectCountNotAsserted` with reasons where attribution is impossible, including PB-84. C9-P1 covers all 6 C9 vectors and rejects fabricated zero or false resource facts. |
| C10 | **conforms** | The implementation-neutral provider builds without either stack, transcodes the published contract, and uses the base-library CBOR codec. The gate found two resolved libraries and no Reference or Minimal dependency. C10-P1 ranges over all 3 C10 vectors and can fail on category drift or any shared semantic runtime. Native both-direction execution remains outside this neutral-only re-review. |

## Prior finding dispositions

### B1 — closed

Decision 11 is now consistent throughout the neutral contract. Negotiation refuses a provider
mismatch as `unsupported-contract`; `provider` and `selectedProvider` are read from the offered
document; `composition-handoff.json` says the same; and PB-78 now requires negotiation itself to
refuse substitution before any plan exists. The Composition step-6 check remains for the distinct
case in which a provider-unspecific requirement was resolved to a different provision.

### B2 — closed

The contract chose the fail-closed interpretation. Withdrawal ends the outstanding request locally;
no new request or Outcome is legal in `withdrawn`, no `withdrawn --outcome--> ...` transition is
declared, and the Channel envelope keeps `outcome` legal only in `active`. PB-84 pins a late Outcome
as `state-violation`, forbids fabricated success, and records the effect count as unknown.

### B3 — closed

`binding-observation.json` now declares `providerEffectCount` as
`optional<Integer.Signed64>` with absent form `unknown`, while retaining the field on every terminal
observation. The completeness rule forbids rewriting unobservability as zero and requires a known
count for success. A targeted inventory found 65 vectors with an explicit effect form: 55 known
counts and 10 reasoned `effectCountNotAsserted` cases, with no vector carrying both forms, neither
form where one was required, a negative count, or an empty rationale.

### B4 — closed

`capability-properties.json` declares exactly one property for each C1-C10, each with scope
`all-vectors-with-capability`, a substantive statement, and a concrete counterexample. The scopes
bind automatically to future vectors carrying the capability. Independent inspection found the
current coverage to be C1=15, C2=15, C3=10, C4=16, C5=14, C6=11, C7=4, C8=18, C9=6, and C10=3.

The properties are sufficient rather than restated examples: they express establishment/effect
atomicity, plan existence and immutability, authority non-transfer, single-result classification,
Shape safety, resource truthfulness, parity, lifecycle/bound enforcement, attributable
observations, and neutral-runtime independence. Each can fail under its named trigger. Group-local
Catalog and handoff properties remain useful narrower assertions but are no longer mistaken for the
capability-wide requirement.

### N1 — closed as reported

The three reported PB8-facing statements now say both stacks target Architecture 0.8:
`binding/portable/contract-matrix.md`, the Portable Binding section of
`docs/current/policies/public-boundaries.md`, and `binding/neutral-provider/README.md`. The gate also
checks the relevant target-claim documents for the superseded Architecture 0.7 target phrases.

### N2 — closed

`conformance/channel-0.1-vectors.json` now says all vectors have executed through both stack
harnesses while retaining `not ratified`. The Channel ledger defines `vectors-authored` as not yet
executed, records CH-R11 as `realisation-executed`, says all 24 vectors have independent stack
evidence, and distinguishes evidence from ratification. Its forward-scenario introduction now says
the targets are not implemented *in the ledger itself*, while the individual scenarios accurately
identify evidence delivered by the Portable Binding realization.

## New finding

### N3 — nonblocking contract semantics; blocking PB8 documentation closure — current policy header names the superseded governing architecture

`docs/current/policies/public-boundaries.md` lines 3-6 describes itself as the operational contract
for the Architecture 0.5 baseline and experiments, then says **“New architectural decisions come
from Architecture 0.7.”** The same document's Portable Binding section correctly says both stacks
target Architecture 0.8, and the status registry selects Architecture 0.8 as current. A reader using
this current policy as directed is therefore sent to an historical architecture for new decisions.
The header should point to the registry-selected current architecture (or explicitly delimit which
historical portions remain governed by 0.7) before PB8 documentation closure is claimed.

## Commands and results

Commands ran from the root of the isolated detached snapshot unless stated otherwise.

1. `git clone --no-hardlinks --no-checkout <local-repository> <system-temp>` followed by
   `git checkout --detach c6f9d51`
   - **Pass.** `HEAD` exactly matched
     `c6f9d51d88e2ce6a7f44042ca507cc67979e7d21`; the review snapshot ended clean.
2. `Get-FileHash -Algorithm SHA256 docs/current/architecture/Brontide-Architecture-0.8.md`
   - **Pass.** The hash matched `Brontide-Architecture-Status.json`.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First sandboxed run: all neutral data checks passed, then NuGet restore failed with `NU1301`
     because sandbox network access was denied.
   - Approved rerun: **Pass.** `9 neutral schemas, 84 vectors covering 10 capabilities and 24
     Channel vectors, and 6 re-derived golden encodings`; neutral-provider build completed with 0
     warnings and 0 errors; dependency inspection found 2 resolved libraries and none from either
     stack. Native evidence was skipped as requested by `-NeutralOnly`.
4. Read-only property/effect inventory over every JSON file under `binding/portable/vectors/`
   - **Pass.** 84 unique vectors; exactly one global property per capability; the per-capability
     counts are recorded under B4. Sixty-five vectors state an effect form and the 10 unknown cases
     each carry a non-empty attribution rationale.
5. Targeted semantic probes over Decision 11/provider-source rules, lifecycle states and transitions,
   Channel legal states, PB-78/PB-84 expectations, observation absent-form rules, architecture-target
   statements, and Channel execution metadata
   - **Pass for B1-B4 and N1-N2.** The initial probe draft selected the wrong nested Binding Plan
     fact collection and matched the retained negative attestation; after correcting those reviewer
     queries, both provider facts were found to use the offered document and the current target
     corpus contained none of the reported stale target phrases.
   - **New finding N3.** A broader manual read of the same current policy found its still-stale
     governing-architecture header.

## Attestation decision

All C1-C10 neutral-contract decisions conform or have the explicitly approved C6 disposition, and
the original B1-B4/N1-N2 findings are closed at the pinned commit. PB8 Step 5 neutral review closure
is still blocked by N3, an in-scope current-documentation inconsistency. Overall attestation:
**does-not-conform**.
