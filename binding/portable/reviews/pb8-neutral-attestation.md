# PB8 neutral Portable Binding attestation

## Review identity and scope

- **Reviewer identity:** `agent:pb8-neutral-review-2026-08-11`
- **Review date:** 2026-08-11
- **Fresh context:** `true`
- **Implementation context access:** `none`
- **Implementation actor:** no
- **Pinned commit:** `ab94ad742104f6939b4a378373f2d68b285c3751`
- **Snapshot:** fresh local clone in a system temporary directory, checked out detached at the pinned commit; the snapshot was clean before and after review
- **Scope:** PB8 Step 5, implementation-neutral Portable Binding contract only
- **Excluded:** Reference and Minimal implementation correctness, native/cross-stack execution, fixes, and every other review or attestation

I did not inspect or rely on another reviewer's output. I reviewed the complete selected architecture,
the complete Portable Binding plan and neutral contract corpus, the Channel ledger and vectors, the
public-boundary statements, and the implementation-neutral provider boundary. I made no semantic or
implementation changes.

## Architecture and programme status

`Brontide-Architecture-Status.json` selects **Brontide Architecture 0.8**, status **Complete Draft
(document and implementation evidence complete; not ratified)**. Its registered SHA-256
`6D844F5FA4D0D3CF09188765A912A13B30889A6ED1F232A28351F179016A8B2F` matches the complete selected
document in the pinned snapshot. The registry reports **no latest ratified architecture**.

Portable Binding 0.1 is experimental work designed for Architecture 0.8 sections 16 and 18.1. It is
not Brontide Base, does not ratify Architecture 0.8, Channel, Composition, or its deterministic-CBOR
wire, and is not a stable public contract. PB0 through PB7 are reported complete; PB8 is partly
complete, with fresh independent review and final question closure outstanding at this commit.

The declared limitations remain material:

- the resource floor is a copied immutable blob plus an addressing-only handle; borrow intervals,
  ownership transfer, observable release/reuse, and fallback are outside 0.1;
- retry, cancellation, streaming, ordering, and exactly-once execution are non-promises;
- the binding assumes an already-connected duplex, locally selected executable, trusted account and
  launcher, and provides no cryptographic peer identity or multi-tenant isolation;
- the PB7 handoff accepts only already-resolved `1..1` distinct exposure and does not implement
  discovery, acquisition, provider selection, generations, Provider Sets, mediation, or hot swap;
- provisional Channel Shape/category naming still awaits an architecture-maintainer ruling and
  blocks a stable public Portable Binding version.

## Overall verdict

**does-not-conform**

The data-only gate passes, but the published neutral contract is internally contradictory and does
not satisfy the repository's property-per-capability rule. Findings B1-B4 are blocking for PB8
neutral-contract closure. Passing structural validation and coverage accounting cannot establish
conformance while mutually incompatible rules remain valid contract text.

## C1-C10 decisions

Exactly one verdict is recorded for each capability.

| Capability | Verdict | Rationale and evidence |
| --- | --- | --- |
| C1 | **does-not-conform** | Exact establishment, fail-closed unknown handling, compact-id scoping, and PB-83 are structurally covered. However PB-78 still requires provider-substituted negotiation to succeed because provider identity is allegedly never compared, contradicting Decision 11, `component-contract.json`, and PB-83, which require negotiation to refuse that mismatch. C1 also has no property quantified over all 15 C1 vectors (B1, B4). |
| C2 | **does-not-conform** | The immutable/inspectable plan and handoff facts are defined, but `schemas/composition-handoff.json` says the plan's provider fact is read from the required document. `schemas/binding-plan.json` and Decision 11 require the offered document so the fact names who answered. PB-78 preserves the superseded negotiation rule. C2 has no property over all 15 C2 vectors (B1, B4). |
| C3 | **does-not-conform** | No-capability-transfer, frameless denial, strong-Kleene evaluation, and zero-effect authority refusals are coherently declared and covered. The only explicit C3 property is `CATALOG-P2`, which ranges only over the Catalog group, not all 10 C3 vectors; the mandatory capability-wide property is absent (B4). |
| C4 | **does-not-conform** | The envelope, 12 protocol categories, 7 process categories, 5 failure domains, correlation rules, and four-way failure separation match the 24 Channel vectors, and the gate covers them. `CATALOG-P3` ranges only over the Catalog group, not all 16 C4 vectors, so the capability-wide property is absent (B4). Channel-vector status metadata is also stale (N2). |
| C5 | **does-not-conform** | The Shape floor, deterministic representation, additive payload projection, strict authority positions, and adversarial cases are coherent and golden encodings re-derive. `CATALOG-P3` covers only Catalog vectors, not all 14 C5 vectors, so the capability-wide property is absent (B4). |
| C6 | **does-not-conform** | The narrower 0.1 evidence is honestly dispositioned: representation, scope, access, ownership, and integrity are exercised; borrow interval, lifetime/reuse, release, and fallback remain declared-but-unexercised and require a future lifetime-bearing flavor. That limitation is an approved disposition, but the capability verdict is still does-not-conform because `CATALOG-P2` is group-scoped and no property ranges over all 11 C6 vectors (B4). |
| C7 | **does-not-conform** | The parity profile clearly separates compared and realization-dependent fields, and C7 has four vectors. There is no explicit C7 property at all, much less one over every C7 vector (B4). Neutral-only review does not independently re-run the native parity matrix. |
| C8 | **does-not-conform** | Bounds, replay, lifecycle, decoder hardening, and release gating are declared. The lifecycle nevertheless says an outstanding Outcome may be accepted after withdrawal while `outcome` is legal only in `active`, the invariant says the same, and no `withdrawn` Outcome transition exists. The two explicit C8 properties cover only the handoff group, not all 17 C8 vectors (B2, B4). |
| C9 | **does-not-conform** | The observation schema defines provider, representation, boundaries, copies, authority point, terminal state, correlation, resources, and timing. It also makes integer `providerEffectCount` required on every terminal observation and says every failure path reports zero. Nine neutral vectors correctly state the effect count is unknowable and must not be asserted; no absent/unknown form exists in the schema. C9 has no explicit property (B3, B4). |
| C10 | **does-not-conform** | The independent-provider boundary is real: the neutral gate built it and verified two resolved libraries, neither from Reference or Minimal. C10 has three vectors but no property over them, contrary to Decision 10 and the standing repository rule (B4). Native both-host-direction execution is outside this neutral-only attestation. |

## Findings

### B1 — blocking — Decision 11 is contradicted inside the neutral contract (C1, C2)

Decision 11 says negotiation compares provider identity, refuses mismatch as `unsupported-contract`,
and reads plan provider facts from the offered document. This is correctly stated by:

- `binding/portable/schemas/component-contract.json` `negotiation.providerIdentityRule`;
- `binding/portable/schemas/binding-plan.json` facts `provider` and `selectedProvider`; and
- `binding/portable/vectors/establishment-and-shapes.json` PB-83.

It is contradicted by two still-authoritative neutral artifacts:

- `binding/portable/schemas/composition-handoff.json` stage `interconnection` says the provider fact
  is read from the required document; and
- `binding/portable/vectors/composition-handoff.json` PB-78 says negotiation succeeds because 0.1
  never compares provider identity.

Thus two consumers can follow different published rules. The structural gate accepts both because
it validates form and coverage, not semantic agreement among prose-bearing data fields.

### B2 — blocking — post-withdrawal Outcome acceptance is impossible in the declared lifecycle (C8)

`limits-and-lifecycle.json` defines `withdrawn` as allowing an outstanding Outcome to be accepted
and permits `active -> withdrawn`. The same file states an Outcome is legal only in `active` and has
no `withdrawn --outcome--> ...` transition. `channel-envelope.json` likewise permits `outcome` only
in `active`. The contract therefore promises a post-withdrawal observation that its state machine
must reject. The intended destination and terminal/withdrawn semantics are not inferable safely.

### B3 — blocking — C9 requires a fabricated effect count where the vectors forbid one (C9)

`binding-observation.json` declares `providerEffectCount` as a required `Integer.Signed64`, says a
failure path must report zero, and requires every normative field on every terminal observation.
It declares no absent or unknown representation for this field. Nine neutral vectors instead use
`effectCountNotAsserted` because timeout, interruption, peer termination, uncorrelated frames, and
similar observations cannot establish whether the provider performed an effect. The contract matrix
explicitly says reporting zero would fabricate knowledge. The observation schema and vectors cannot
both be satisfied.

### B4 — blocking — no capability has the required property over all its vectors (C1-C10)

The plan's Decision 10 claims every capability states at least one property holding over all its
vectors, and `AGENTS.md` requires a property per capability over all of that capability's vectors.
The neutral corpus contains only six properties:

- three in `catalog-vectors.json`, each quantified only over that group; and
- three in `composition-handoff.json`, each quantified only over that group.

The probe found 83 unique vectors. C7, C9, and C10 have no explicit property at all. C1-C6 and C8
appear on one or more group properties, but each property explicitly ranges only over that group,
while those capabilities also have vectors in other files. None therefore states an invariant over
all vectors of the capability. The gate counts vector/category coverage but does not validate this
standing completeness requirement.

### N1 — nonblocking contract semantics; blocking PB8 documentation closure — architecture target text is stale

The status registry and plan say both stacks now target Architecture 0.8. The following PB8-facing
documents still say the work changes neither stack's Architecture 0.7 target:

- `binding/portable/contract-matrix.md` status;
- `docs/current/policies/public-boundaries.md` Portable Binding section; and
- `binding/neutral-provider/README.md` boundary.

This does not change the neutral wire semantics, but PB8 requires current limitations and accurate
architecture-target language, so it must be corrected before documentation closure.

### N2 — nonblocking contract semantics; blocking PB8 documentation closure — Channel execution metadata is stale

`conformance/channel-0.1-vectors.json` still reports `stack harnesses pending; not ratified`, while
the Channel ledger records CH-R11 as `realisation-executed` and the neutral gate reports coverage of
all 24 vectors. The Channel ledger's forward-scenario introduction also says none is implemented
immediately before marking scenarios delivered or partly delivered. Ratification remains correctly
absent; execution status is the stale part.

## Commands and results

Commands ran from the repository root of the isolated detached snapshot unless stated otherwise.

1. `git clone --no-hardlinks --no-checkout <local-repository> <system-temp>; git checkout --detach ab94ad742104f6939b4a378373f2d68b285c3751`
   - **Pass.** `HEAD` exactly matched the pinned commit; `git status --short` was empty.
2. `Get-FileHash -Algorithm SHA256 docs/current/architecture/Brontide-Architecture-0.8.md`
   - **Pass.** Hash matched the status registry.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-portable-binding.ps1 -NeutralOnly`
   - First attempt: neutral data checks passed, then NuGet restore was blocked by sandbox network
     policy (`NU1301`).
   - Approved rerun: **Pass.** `9 neutral schemas, 83 vectors covering 10 capabilities and 24
     Channel vectors, and 6 re-derived golden encodings`; neutral provider build succeeded with 0
     warnings and 0 errors; dependency inspection found 2 resolved libraries and none from either
     stack. Native evidence was skipped as requested by `-NeutralOnly`.
4. Data-only JSON inventory over every file in `binding/portable/vectors/`
   - **Pass as an inventory.** 83 vectors, 83 unique ids; 9 vectors use
     `effectCountNotAsserted`; per-capability counts were C1=15, C2=15, C3=10, C4=16, C5=14,
     C6=11, C7=4, C8=17, C9=5, C10=3.
   - **Finding.** Explicit group-property counts were C1=1, C2=3, C3=1, C4=1, C5=1, C6=1,
     C7=0, C8=2, C9=0, C10=0; every present property is group-scoped rather than capability-wide.
5. Targeted read-only searches for provider-source rules, withdrawal/Outcome transitions,
   effect-count requirements, architecture-target statements, and property declarations
   - **Findings B1-B4 and N1-N2 reproduced** at the paths described above.

## Attestation decision

The neutral Portable Binding evidence at the pinned commit is substantial and its mechanical gate is
green, but PB8 Step 5 cannot close for the neutral contract. Findings B1-B4 are unresolved in-scope
contract defects. Overall attestation: **does-not-conform**.
