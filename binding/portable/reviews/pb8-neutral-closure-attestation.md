# PB8 neutral Portable Binding closure attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-neutral-closure-review-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `5150d6d774d683a6ce8e769f7472724d40f0baba`
- **Snapshot:** fresh isolated local clone, checked out at the exact pinned commit; the review used
  the clone rather than the shared mutable worktree as evidence
- **Scope:** PB8 Step 5 closure review of the implementation-neutral Portable Binding contract,
  schemas, vectors, golden encodings, capability-wide properties, Channel mapping and evidence
  wording, neutral-provider boundary, and current architecture and target declarations
- **Retained history:** the three earlier neutral attestations were read only to identify and
  independently disposition B1-B4 and N1-N3; the retained native attestations identify the R1/F1
  effect-attribution defect class but do not substitute for this neutral review
- **Excluded:** Reference- and Minimal-native implementation correctness, native execution, and
  cross-stack runtime correctness; those are separate realization-review scopes

I am not an implementation actor and had no access to an implementation session's private
reasoning. I made no change to the isolated snapshot. This attestation is the only file I wrote in
the shared worktree.

## Architecture, stack targets, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, which matches the registry.
The registry reports **no latest ratified architecture**.

Architecture 0.8 requires portable Shape identity and structure, additive projection only in
payload positions, non-projecting fail-closed authority positions, explicit operational truth at
representation and trust boundaries, and implementation independence. Composition and Binding
Plans remain extracted design direction outside Base, and Channel remains provisional rather than
ratified architecture.

Both `Reference/README.md` and `Minimal/README.md` state **Designed for Architecture 0.8, Complete
Draft, not ratified**, and qualify that target as a partial implementation with explicitly labelled
experiments. The current public-boundary policy directs new architectural decisions to the
registry-current Architecture 0.8 while retaining the Architecture 0.5 evidence-baseline boundary.

Portable Binding 0.1 remains experimental Architecture 0.8 section 16/18.1 work. It does not
ratify Architecture 0.8, Channel, Composition, deterministic CBOR, or a stable public extension.
Its relevant limits remain explicit: copied immutable blobs and an addressing-only handle are the
exercised resource floor; borrowing, transfer, lifetime/reuse, release signalling, and fallback are
not exercised capabilities; retry, cancellation, ordering, streaming, and exactly-once execution
are non-promises; the process seam assumes an already-connected duplex and trusted launcher and
does not provide cryptographic peer identity or multi-tenant isolation; and the Composition handoff
is limited to an already-resolved, distinct `1..1` exposure. Decision 13 assigns the wider
relational lifecycle contract to version 0.2.

## Overall verdict

**conforms**

All C1-C10 neutral-contract decisions conform or carry the explicitly bounded C6 approved
disposition. The capability-wide properties are substantive and falsifiable, the NeutralOnly gate
is green, B1-B4 and N1-N3 remain closed, and the retained R1/F1 effect-attribution class exposes no
remaining neutral-contract gap. No new implementation-neutral finding remains.

This verdict closes only the neutral PB8 review scope. It neither ratifies architecture or Channel
nor adjudicates the separate native realization reviews.

## C1-C10 decisions

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact establishment precedes provider effect, unknown declarations fail closed, compact identifiers are binding-scoped, and provider identity is compared. PB-78/PB-83 and Decision 11 agree. C1-P1 applies to all 15 C1 vectors and can fail if refusal produces a plan or effect. |
| C2 | **conforms** | A plan exists only after successful negotiation and remains immutable. Its provider facts name who answered and are read from the offered document. C2-P1 applies to all 15 C2 vectors and names partial-plan and mutation counterexamples. |
| C3 | **conforms** | Capability custody stays local; authority-bearing content cannot cross the trust seam; denied and unknown authority are frameless and pre-effect. C3-P1 applies to all 10 C3 vectors and can fail on transfer or denied effect. |
| C4 | **conforms** | Envelope kinds, correlation, protocol/process categories, relative failure domains, and separation of denial, Outcome, protocol failure, and process failure cover all 24 Channel vectors without ratifying Channel. C4-P1 applies to all 16 C4 vectors and forbids fabricated success or competing categories. |
| C5 | **conforms** | The Shape floor, strict required structure, declared Fragments, additive payload projection, deterministic representation, and non-projecting authority positions agree with Architecture 0.8 section 16. C5-P1 applies to all 14 C5 vectors and names malformed and authority-bearing triggers. |
| C6 | **approved-disposition** | The exercised 0.1 resource floor coherently covers representation, scope, access, ownership, and integrity. Borrow interval, lifetime/reuse, release, and fallback are explicitly declared but unexercised until a lifetime-bearing flavor exists. C6-P1 applies to all 11 C6 vectors and prevents false acceptance, effects, and resource facts. |
| C7 | **conforms** | The parity profile separates compared facts from realization-dependent facts, and C10's neutral-provider boundary excludes either stack runtime. C7-P1 applies to all 4 C7 vectors and can fail on disagreement in any compared field. |
| C8 | **conforms** | Limits, replay, and lifecycle transitions fail closed. Outcome is legal only in `active`; withdrawal removes the outstanding request; PB-84 refuses a late Outcome as `state-violation` and makes effect attribution unknown. C8-P1 applies to all 18 C8 vectors and names illegal-state, replay, and bound triggers. |
| C9 | **conforms** | `providerEffectCount` is a required optional value with absent form `unknown`; a known count is non-negative and success requires a known count. C9-P1 applies to all 6 C9 vectors and forbids fabricated zero and false resource facts. The complete effect inventory is detailed below. |
| C10 | **conforms** | The neutral provider builds from the published contract without a Reference or Minimal dependency. The gate found two resolved libraries and neither stack. C10-P1 applies to all 3 C10 vectors and can fail on category drift or shared semantic runtime. |

Exactly one `all-vectors-with-capability` property exists for each C1-C10, with a substantive
statement and concrete counterexample. Current coverage is C1=15, C2=15, C3=10, C4=16, C5=14,
C6=11, C7=4, C8=18, C9=6, and C10=3. The scope rule automatically binds a future vector carrying
the capability.

## Finding dispositions

### B1 - closed

Decision 11 is coherent across the neutral artifacts. Negotiation refuses a provider mismatch as
`unsupported-contract`; `provider` and `selectedProvider` are read from the offered document; and
PB-78/PB-83 refuse substitution before a plan or effect. The Composition step-6 check remains the
distinct fail-closed check that the answering provision is the one resolution selected.

### B2 - closed

The lifecycle consistently uses the fail-closed interpretation. Outcome is legal only in `active`,
there is no `withdrawn --outcome--> ...` transition, withdrawal ends the outstanding request, and
PB-84 refuses a late Outcome as `state-violation` without fabricating success or a zero effect count.

### B3 - closed

The observation schema represents `providerEffectCount` as
`optional<Integer.Signed64>` with absent form `unknown`, retains it as a required normative field,
forbids rewriting unobservability as zero, and requires a known count for success.

The targeted inventory found 65 of 84 unique vectors with an explicit effect form: 55 declare the
known count `0`, and 10 declare a non-empty `effectCountNotAsserted` rationale. No vector declares
both forms, no known count is negative, and every adversarial denial, protocol error, or process
failure declares one form.

The distinction is coherent with the stage at which knowledge is lost, rather than being guessed
from the portable category alone. Pre-effect contract, payload, authority, limit, replay, and
lifecycle refusals use known zero. PB-40, PB-41, PB-43, PB-44, PB-45, PB-49, PB-50, PB-51, PB-52,
and PB-84 use unknown because timeout, interrupted transport, malformed or missing correlation,
unrecognised response framing, endpoint failure, peer/process loss, observer relativity, or a late
Outcome prevents attribution. In particular, `malformed-message` is known zero for pre-effect
contract/control parsing (for example PB-05/PB-06/PB-08/PB-17) but unknown for PB-44's post-request
Outcome without correlation; `state-violation` is known zero when the refused action cannot begin a
new effect (for example PB-09/PB-36/PB-37/PB-39) but unknown for PB-84 after an earlier request may
already have produced an effect. This is the contract distinction the retained R1/F1 realization
findings required.

### B4 - closed

`capability-properties.json` contains exactly one global property for each C1-C10. Every property is
scoped to all vectors carrying the capability, states a capability invariant rather than one
example, and names a trigger that can make it fail. Group-local Catalog and Composition properties
remain narrower supporting evidence rather than substitutes.

### N1 - closed

The contract matrix, Portable Binding and neutral-provider boundaries, public-boundary Portable
Binding section, selected architecture, and both stack target declarations consistently state
Architecture 0.8 without upgrading ratification language.

### N2 - closed

Channel metadata says all 24 shared vectors executed through both stack harnesses and preserves
`not ratified`. The ledger defines and applies `realisation-executed`, records CH-R11 with that
disposition, and distinguishes delivered Portable Binding evidence from remaining Channel targets.

### N3 - closed

`docs/current/policies/public-boundaries.md` directs new architectural decisions to current
Architecture 0.8 and explicitly says this does not ratify the Complete Draft. The stale direction to
Architecture 0.7 is absent, and the neutral gate guards the wording.

### R1/F1 effect-attribution class - closed in the neutral scope

The retained realization reviews found two reachable native paths that converted unobservable
provider effects to known zero: process loss and a post-request correlation refusal. Those findings
did not require a neutral-contract amendment. The neutral schema already mandates `unknown`, PB-50
already classifies peer termination as `effectCountNotAsserted`, and PB-43 already classifies the
mismatched Outcome the same way. The broader inventory above confirms that these are not isolated
exceptions and that the known-zero cases remain distinguishable by observable pre-effect evidence.

Native correction correctness remains for the Reference and Minimal closure reviewers; this
implementation-neutral attestation records that R1/F1 leaves no unresolved neutral declaration,
vector, property, or completeness gap.

## Commands and results

Commands ran from the isolated pinned snapshot unless stated otherwise.

1. `git clone --no-hardlinks <local-repository> <system-temp>; git checkout 5150d6d; git rev-parse HEAD`
   - **Pass.** `HEAD` was exactly
     `5150d6d774d683a6ce8e769f7472724d40f0baba`; the initial snapshot was clean.
2. Registry and SHA-256 probe for the status-selected architecture
   - **Pass.** Revision 0.8 and its registered document hash matched; the registry reports no
     ratified architecture.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First sandboxed run: neutral structural validation passed, then NuGet restore failed with
     `NU1301` because network access was denied.
   - Approved rerun: **Pass.** 9 schemas, 84 vectors covering C1-C10 and all 24 Channel vectors, and
     6 re-derived golden encodings; neutral-provider build completed with 0 warnings and 0 errors;
     dependency inspection found 2 resolved libraries and neither stack. Native evidence was
     skipped as required by `-NeutralOnly`.
4. Read-only targeted PowerShell assertions over the status registry, all neutral vector files,
   capability properties, provider-source declarations, lifecycle transitions, Channel legal
   states, observation effect forms, target wording, and Channel execution metadata
   - **Pass:** `TARGETED_PROBES_PASS architecture=0.8 vectors=84 properties=10 knownZero=55
     unknown=10 adversarialFailures=51 capabilityCounts=C1=15,C2=15,C3=10,C4=16,C5=14,C6=11,C7=4,C8=18,C9=6,C10=3`.
5. Manual classification review of every explicit known-zero and unknown-effect vector, including
   same-category cases separated by pre-effect versus post-request observability
   - **Pass.** All 10 unknown cases have a concrete non-attribution reason; all 55 known-zero cases
     assert either no provider effect began or no new effect occurred. No inconsistent pair or
     unclassified adversarial failure was found.

## Attestation decision

The implementation-neutral Portable Binding evidence at the pinned commit satisfies C1-C10 within
its explicit experimental 0.1 boundary. B1-B4, N1-N3, and the neutral side of the retained R1/F1
effect-attribution class are closed; required neutral verification is green; and no unresolved
in-scope finding remains. Overall attestation: **conforms**.
