# Portable Binding — implementation-neutral contract matrix (C1–C10)

**Status:** PB0 baseline inventory complete for the C1–C10 mapping. Neutral vectors are authored in
PB1. Capability text summarises plan §2 and is subordinate to the plan.

**Owner legend:** CM = Portable Binding contract maintainers · Ref = Reference stack owner ·
Min = Minimal stack owner · Both = CM + both stack owners.

**Classification** (plan §5 PB0): **reusable** = existing behavior carries into the neutral contract
as-is · **normalize** = reusable meaning but the two experiments encode it differently and must be
unified · **fixture-specific** = present only as a narrow test instrument · **missing** = required by
the C-item but absent · **blocked** = gated on an open owner decision (see `open-decisions.md`).

| Capability | Owner | Existing Cooling (v2) / Catalog (v1) basis | Classification | Gap to close → PB phase |
| --- | --- | --- | --- | --- |
| C1 — neutral contract established before effect; unknowns fail closed | CM | Both manifests declare component/provider/operation/shape/fragment/dependency identities with versions; `protocol-error: unsupported-protocol` on version skew; unknown-field/variant vectors reject | reusable + **missing** | Binding-scoped compact identifiers (assigned post-negotiation, never identity) are absent → PB1 |
| C2 — one immutable, inspectable Binding Plan | CM | Cooling `binding{representations,crossedBoundaries,limitations}`; Catalog `resourceBoundary`,`payloadLimitBytes` — plan facts exist but are scattered across manifest fields | **normalize** (no consolidated plan object) | Define one immutable Binding Plan fixing contracts/endpoints/authority-mode/payload-rep/resource-ownership/sync/limits/lifecycle → PB1 |
| C3 — authority stays local; no Capability crosses | Both | `no-capability-transfer` limitation; `authority.hostDecisionRequired`,`constraintPolicy:fail-closed`; host `denial` emitted **before** provider activation; "No Capability is serialized" | **reusable** | Generalise the cross-trust `no-capability-transfer` declaration into the neutral contract → PB1 |
| C4 — Channel 0.1 envelopes, correlation, error categories, process-failure | CM | Message kinds `activate`/`invoke`/`denial`/`protocol-error`/`shutdown`; correlation `requestId`/`executionId`/`occurrenceId`; error codes `unsupported-protocol`,`replay`,malformed/unknown-field/unknown-variant; failed Outcome distinct from protocol-error distinct from process exit | **fixture-specific** (two divergent JSON-lines protocols) | Map both onto `conformance/channel-0.1-vectors.json`; length-delimited bounded wire (JSON-lines → diagnostic only) → PB1/PB4 (wire resolved: deterministic CBOR core) |
| C5 — portable Shape floor | CM | Record Shapes (open/closed fragment policy), scalars `Text`/`Boolean`/`Integer.Signed64`, nested records, sequences (`items[]`,`tags[]`), required fields, declared Fragment (`host-context`), additive projection (optional `failureMode`) | **reusable + normalize** | Cooling uses `{name,version}` refs, Catalog uses `name@version` strings — unify; add Constraint-value **exemption** from additive projection (D2/C8) → PB1 |
| C6 — inline + referenced payloads | Both | Inline = `inline-tagged-json`; Catalog provider-scoped resource handle (`{provider,id}`, accepts `catalog-sandbox/shared`, else `resource-refused`, addressing-only); Cooling `no-referenced-resources` | **fixture-specific** | Define the referenced-shaped-resource v0.1 floor (representation/scope/access/ownership/lifetime/release/integrity/fallback) → PB1/PB6 (floor resolved: copied immutable blob) |
| C7 — independent impls; direct/process parity | Ref, Min | Both stacks parse the same fixtures into independent types; zero shared runtime; boundary/assembly guards; process realization proven (`crossedBoundaries:["process"]`) | reusable + **missing** | Add an explicit **direct-call** realization and a category-level direct-vs-process parity check → PB2/PB3/PB4 |
| C8 — bounded lifecycle + declared limits | CM | Limits: Cooling depth 64 + 10 s I/O timeout (no byte/replay); Catalog 65 536 bytes + depth 32 + per-session replay window. Lifecycle: activate→invoke→(denial\|result\|failure)→shutdown; `single-invocation` | **normalize** (limits inconsistent; lifecycle states implicit) | Unify the declared limit set; enumerate explicit establishment/readiness/invocation/withdrawal/termination states → PB1/PB6 |
| C9 — attributable observations | CM | `providerEffectCount` (result field), `crossedBoundaries`, `executionId`/`occurrenceId`, host-enrichment/boundary provenance, failure-domain distinction | **fixture-specific** ("universal form of binding observations… not ratified") | Define the unified C9 observation set (selected provider, representation, copies, authority point, retry, terminal status, timing…) → PB1 |
| C10 — executable interop evidence | Both | Ref-hosts-Min and Min-hosts-Ref both pass; independence guards enforce no shared types | reusable + **missing** | Add an **implementation-neutral** provider/fixture depending on neither stack → PB5 |

## Baseline inventory: existing neutral surface

### Cooling proof — `interchange/` (protocol v2)

- **Transport:** UTF-8 JSON-lines (one complete JSON object per line); stdin/stdout carry protocol, stderr diagnostic.
- **Manifest (`manifest-v2.json`):** structured `{name,version}` references throughout; `operations[]` with `inputShape`/`outputShape`/`requiredFragments`/`authority{hostDecisionRequired,constraintPolicy:"fail-closed"}`; `shapes[]` (`kind:"record"`, `fragmentPolicy:"open"|"closed"`, `fields[{name,shape,required}]`); `fragments[]` (`hostShape`,`fields`); `dependencies[]` (`kind:"profile"|"binding"`,`strength`,`providerSpecific`); `binding{representations:["inline-tagged-json"],crossedBoundaries:["process"],limitations:["single-invocation","no-capability-transfer","no-referenced-resources"]}`.
- **Message kinds (`messages/`):** `activate` (carries manifest + `requestId`), `invoke`, `denial` (`requestId`,`executionId`,`occurrenceId`,`reason`), `protocol-error` (`code`,`message`), `shutdown`.
- **Scalars:** `Text`, `Boolean`, `Integer.Signed64`. **Fragment policy:** open (command) / closed (result, details).
- **Limits:** JSON depth 64; 10 s per-I/O timeout. **No** byte-size limit; **no** replay window.
- **Values (`values/`):** valid-command, valid-command-with-optional-fragment, valid-result, failed-details, invalid-command-missing-fragment, invalid-command-wrong-kind, invalid-private-type.

### Catalog / resource proof — `interchange/catalog/` (protocol v1)

- **Manifest (`manifest-v1.json`):** flat `name@version` string references; `resourceBoundary:"provider-scoped-resource-handle"`; `payloadLimitBytes:65536`; two operations `upsert-items`, `find-items`.
- **Values:** batch of nested items with repeated tags; one provider process holds ephemeral ordered state; no persistence after exit.
- **Failure:** shaped `failed` Outcome `missing-items` with repeated missing IDs (no exception/private diagnostic crosses).
- **Resource handle:** `{provider,id}`; only `catalog-sandbox/shared` accepted, else protocol `resource-refused`; addressing only, never authority.
- **Replay:** per-endpoint `HashSet<requestId>`; reused id → code `replay`; window ends with the provider process.
- **Limits:** 65 536 encoded bytes/line; JSON depth 32.
- **Adversarial vectors (`catalog/vectors/`):** malformed, payload-limit (65 537 chars), replay, unknown-field, unknown-variant, valid-upsert, version-skew.

### Cross-cutting findings (must reconcile in PB1)

1. **Two manifest encodings.** Cooling v2 uses structured `{name,version}` + a `binding` block; Catalog v1 uses `name@version` strings + flat `payloadLimitBytes`/`resourceBoundary`. The neutral contract needs one reference and one plan encoding.
2. **Two protocol versions with different limit/lifecycle surfaces.** Cooling (v2: timeout, no byte/replay) vs Catalog (v1: byte + depth + replay, no timeout). Unify into one declared limit/lifecycle set.
3. **No consolidated Binding Plan and no unified C9 observation set** — both are explicitly provisional today.
4. **Referenced-resource support is Catalog-only and addressing-only** — not the general C6 form.
5. **Independence is proven; direct/process *parity* is not** — only the process realization exists.

## Channel 0.1 vectors

The Channel envelope / correlation / error vectors that C4 must preserve live in
`conformance/channel-0.1-vectors.json`. PB1 maps each Cooling/Catalog message kind above to the
corresponding Channel vector here.

## Open encoding blockers (PB0 exit criteria)

Both are **owner decisions** and gate PB1. Option sets and recommendations are drafted in
[`open-decisions.md`](open-decisions.md):

1. **Wire representation** — **RESOLVED 2026-07-24: deterministic CBOR core** (RFC 8949 core-deterministic, Catalog byte/depth bounds, canonical-text ids + optional post-negotiation compact ids; JSON-lines diagnostic-only). See `open-decisions.md`.
2. **Referenced-shaped-resource v0.1 floor** — **RESOLVED 2026-07-24: copied immutable blob** (integrity via content hash; Catalog addressing handle retained as a second flavor; borrow/transfer are 0.1 non-goals). See `open-decisions.md`.
3. **Chain-conjunction representation ceiling (D3 / §11)** — **resolved for recording**: [`representation-choice.md`](representation-choice.md) (Reference = carried, Minimal = resolved). Pinned-ledger transcription deferred.
