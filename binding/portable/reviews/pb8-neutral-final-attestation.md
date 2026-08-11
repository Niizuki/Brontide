# PB8 neutral Portable Binding final attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-neutral-final-review-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `fe299c71e2e77199ccdedfba552e193d3e0f91df`
- **Snapshot:** fresh local clone, checked out detached at the pinned commit; the snapshot was clean at
  the end of review
- **Scope:** PB8 Step 5 final review of the implementation-neutral Portable Binding contract,
  schemas, vectors, golden encodings, Channel mapping and evidence wording, neutral-provider
  boundary, current PB8 documentation, and architecture/implementation target declarations
- **Retained closure evidence:** the initial neutral attestation and first neutral re-review only,
  used to identify B1-B4 and N1-N3 for independent closure verification
- **Excluded:** Reference- and Minimal-native implementation correctness, native execution, and
  cross-stack runtime correctness; those remain the separate realization reviews

I am not an implementation actor and had no access to an implementation session's private
reasoning. I inspected an isolated snapshot pinned exactly to the commit above and made no change to
that snapshot.

## Architecture, targets, and limitations

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. The selected document's SHA-256 is
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`, which matches the registry.
The registry reports **no latest ratified architecture**.

The architecture preserves Shape identity, additive payload projection, strict non-projecting
authority positions, fail-closed boundary evaluation, implementation independence, and Channel as
a provisional extension outside Base. Both stack READMEs state **Designed for Architecture 0.8,
Complete Draft, not ratified**, with partial implementations and explicitly labelled experiments.
The current public-boundary policy now directs new architectural decisions to registry-current
Architecture 0.8 while preserving the Architecture 0.5 evidence-baseline qualification.

Portable Binding 0.1 remains experimental Architecture 0.8 section 16/18.1 work and does not ratify
Architecture 0.8, Channel, Composition, deterministic CBOR, or a stable public extension. Its
declared limits remain accurate: copied immutable blobs and an addressing-only handle are the 0.1
resource floor; borrow intervals, transfer, lifetime/reuse, release signalling, and fallback are
not exercised capabilities; retry, cancellation, ordering, streaming, and exactly-once execution
are non-promises; the process seam assumes an already-connected duplex and trusted launcher and
provides no cryptographic peer identity or multi-tenant isolation; and the Composition handoff is
limited to an already-resolved, distinct `1..1` exposure. Decision 13 keeps bounded Relational
Initialisation refused in 0.1 and assigns the wider lifecycle contract to 0.2.

## Overall verdict

**conforms**

All C1-C10 decisions conform or carry the explicitly bounded C6 approved disposition. The
capability-wide properties are substantive and falsifiable, the NeutralOnly gate is green, all
retained findings B1-B4 and N1-N3 are closed, and no new implementation-neutral finding remains.
This verdict closes only the neutral PB8 review scope; it is not an architecture or Channel
ratification and does not replace the two native realization reviews.

## C1-C10 decisions

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **conforms** | Exact pre-effect establishment, unknown-declaration refusal, post-negotiation compact-id scope, and provider-identity comparison agree across the component contract, Decision 11, PB-78, and PB-83. C1-P1 covers all 15 C1 vectors and can fail if a refused contract creates a plan or effect. |
| C2 | **conforms** | A plan exists only after successful negotiation and its facts are immutable. Both provider facts describe who answered and are read from the offered document. C2-P1 covers all 15 C2 vectors and names partial-plan and mutation counterexamples. |
| C3 | **conforms** | Capability custody remains local, cross-trust presentation forbids Capability transfer, and denied or unknown authority is frameless and pre-effect. C3-P1 covers all 10 C3 vectors and can fail on either authority transport or a denied effect. |
| C4 | **conforms** | Envelope kinds, correlation, protocol/process categories, relative failure domains, and denial/Outcome/protocol/process separation cover all 24 Channel vectors without ratifying Channel. C4-P1 covers all 16 C4 vectors and can fail on fabricated success, competing categories, or lost correlation. |
| C5 | **conforms** | The Shape floor, strict required fields, declared Fragments, additive payload projection, deterministic representation rules, and non-projecting authority positions agree with Architecture 0.8 section 16. C5-P1 covers all 14 C5 vectors and names malformed and authority-bearing counterexamples. |
| C6 | **approved-disposition** | The exercised 0.1 floor coherently covers representation, scope, access, ownership, and integrity. Borrow interval, lifetime/reuse, release, and fallback remain explicitly declared but unexercised because no lifetime-bearing flavor exists; Decision 9 assigns widening to 0.2. C6-P1 covers all 11 C6 vectors and prevents false acceptance, effects, or resource claims. |
| C7 | **conforms** | The parity profile separates compared facts from realization-dependent facts, while C10's neutral-provider boundary excludes either stack's runtime. C7-P1 covers all 4 C7 vectors and can fail on disagreement in any compared field; the separate no-shared-runtime vector remains independently meaningful. |
| C8 | **conforms** | Limits, replay, and lifecycle transitions fail closed. An Outcome is legal only in `active`; withdrawal removes the outstanding request; and PB-84 refuses a late Outcome as `state-violation` without fabricating success or an effect count. C8-P1 covers all 18 C8 vectors and names illegal-state, replay, and over-limit triggers. |
| C9 | **conforms** | `providerEffectCount` is a required optional value whose absent form is `unknown`; known counts are non-negative and success requires a known count. Ten vectors give a concrete non-attribution reason, including timeout, interrupted transport, peer/process loss, and PB-84. C9-P1 covers all 6 C9 vectors and rejects fabricated zero and false resource facts. |
| C10 | **conforms** | The implementation-neutral provider builds from the published contract without a Reference or Minimal project/runtime dependency. The gate inspected two resolved libraries and found neither stack. C10-P1 covers all 3 C10 vectors and can fail on category drift or any shared semantic runtime. |

Each C1-C10 property has scope `all-vectors-with-capability`, a capability-level invariant rather
than a restated example, and a concrete counterexample. The current capability counts are C1=15,
C2=15, C3=10, C4=16, C5=14, C6=11, C7=4, C8=18, C9=6, and C10=3. The scope rule automatically
binds future vectors carrying the capability.

## Finding dispositions

### B1 - closed

Decision 11 is coherent across the neutral artifacts. Negotiation compares provider identity and
refuses a mismatch as `unsupported-contract`; the Binding Plan's `provider` and
`selectedProvider`, and the observation's selected-provider fact, come from the **offered**
document. PB-78 and PB-83 pin mismatch refusal before a plan or provider effect. The Composition
step-6 check remains a distinct fail-closed check that the answering provider is the provision the
resolver selected.

### B2 - closed

The lifecycle uses the fail-closed interpretation consistently. `outcome` is legal only in
`active`; there is no `withdrawn --outcome--> ...` transition; withdrawal ends the outstanding
request; and PB-84 requires a late Outcome to be refused as `state-violation`. The contract no
longer promises an acceptance its state machine forbids.

### B3 - closed

The observation schema represents `providerEffectCount` as `optional<Integer.Signed64>` with absent
form `unknown`, requires the normative field on terminal observations, forbids rewriting absence as
zero, and requires a known count for success. Of 65 unique vectors carrying an explicit effect
form, 55 carry a known non-negative count and 10 carry one non-empty
`effectCountNotAsserted` reason; no vector carries both. The process-loss and late-Outcome vectors
preserve non-attribution rather than inventing zero.

### B4 - closed

`capability-properties.json` contains exactly one global property for each C1-C10. Each is scoped to
all vectors carrying that capability, states a real capability invariant, and names a trigger that
could make it fail. Group-local Catalog and Composition properties remain narrower supporting
checks and are not substituted for the capability-wide properties.

### N1 - closed

The contract matrix, neutral-provider boundary, public-boundary Portable Binding section, and both
stack target declarations consistently state Architecture 0.8 as the local implementation target
without upgrading ratification language.

### N2 - closed

The Channel vector metadata says all 24 shared vectors have executed through both stack harnesses
and still says `not ratified`. The ledger defines `realisation-executed`, records CH-R11 with that
disposition, distinguishes execution evidence from ratification, and says scenarios marked
*Delivered* are implemented and executed by Portable Binding while remaining scenarios are targets.
The superseded statement that none is implemented is absent.

### N3 - closed

`docs/current/policies/public-boundaries.md` now directs new architectural decisions to the current
Architecture 0.8 document and explicitly says that doing so does not ratify the Complete Draft. The
stale direction to Architecture 0.7 is absent, and the neutral gate now guards this wording.

## Commands and results

Commands ran from the root of the isolated detached snapshot unless stated otherwise.

1. `git clone --no-hardlinks --no-checkout <local-repository> <isolated-path>` followed by
   `git checkout --detach fe299c7`
   - **Pass.** `HEAD` was exactly
     `fe299c71e2e77199ccdedfba552e193d3e0f91df`; the snapshot ended clean.
2. `Get-FileHash -Algorithm SHA256 docs/current/architecture/Brontide-Architecture-0.8.md`
   - **Pass.** The hash matched the status registry.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First sandboxed run: neutral structural checks passed, then restore failed with `NU1301`
     because sandbox network access was denied.
   - Approved rerun: **Pass.** `9 neutral schemas, 84 vectors covering 10 capabilities and 24
     Channel vectors, and 6 re-derived golden encodings`; neutral-provider build completed with 0
     warnings and 0 errors; dependency inspection found 2 resolved libraries and none from either
     stack. Native evidence was skipped as requested by `-NeutralOnly`.
4. Read-only targeted PowerShell assertions over the status registry, all neutral vector files,
   capability properties, Decision 11/provider-source declarations, PB-78/PB-83/PB-84, lifecycle
   transitions, Channel legal states, observation effect forms, stack targets, public-boundary
   policy, Channel metadata/ledger, and the neutral-provider project boundary
   - **Pass:** `TARGETED_PROBES_PASS uniqueVectors=84 properties=10 effectVectors=65
     unknownEffectVectors=10 channelVectors=24`.
5. Manual semantic review of C1-P1 through C10-P1, all nine schemas, all neutral vector groups and
   six golden encodings, the Portable Binding plan/matrix/decisions/completeness record, current
   architecture and targets, public-boundary policy, neutral-provider boundary, PB8 measurement
   evidence, Channel draft/note/vector inventory/ledger, and the retained neutral findings
   - **Pass.** No unresolved or new implementation-neutral finding was found.

## Attestation decision

The implementation-neutral Portable Binding evidence at the pinned commit satisfies C1-C10 within
its explicit experimental 0.1 boundary, all retained B1-B4 and N1-N3 findings are closed, and the
required neutral verification is green. Overall attestation: **conforms**.
