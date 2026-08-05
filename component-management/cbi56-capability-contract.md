# CBI56 capability contract — distribution-endpoint key rotation

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI39 authenticates one policy-distribution response under one exact host-pinned endpoint key. CBI56
lets that endpoint authorize a successor without treating the policy authority as the endpoint
authority and without accepting the successor merely because it is advertised. A durable host-local
anchor retains the active endpoint, one optional staged successor, and a monotone generation.

The current endpoint signs a canonical transition to the successor. The host durably stages that
transition, then requires a complete successful CBI39 synchronization authenticated by the staged
key before atomically making it active. Until confirmation succeeds, ordinary synchronization still
uses the current key.

This is cooperative one-host endpoint-key rotation. It is not CBI37 policy-authority rotation,
certificate-chain discovery, endpoint URI discovery, multi-endpoint failover, quorum, transparency
logging, secure-clock provision, hostile rollback custody, or TLS handler policy.

## Capabilities

### C1 — one durable anchor names one active endpoint generation

Opening an absent anchor establishes generation zero under an explicit initial endpoint identity.
Every snapshot retains that initial identity, the active identity, the non-negative generation, and
at most one staged successor. Reopening with another initial identity is refused.

Property: every usable anchor has exactly one active endpoint identity and monotone generation.

### C2 — only the active endpoint can authorize its exact successor

A rotation statement names generation plus one, the exact active predecessor, one distinct successor,
`ECDSA-P256-SHA256`, the predecessor SPKI, and an RFC 3279 DER signature over the canonical CBI56
manifest. The SPKI digest must equal the active identity and import as exact P-256 key material.

Property: no unpinned or invalid signature can stage an endpoint successor.

### C3 — staging is strict, durable, and single-successor

Generation gaps, replay, predecessor mismatch, self-rotation, malformed evidence, and a different
successor while one is staged are refused without changing bytes. An identical already-staged
statement is an idempotent observation. Successful staging is published atomically before any
successor network attempt.

Property: every staged successor is the unique generation-plus-one transition from the active pin.

### C4 — ordinary synchronization keeps using the active endpoint

The anchor creates ordinary CBI39 clients only from its active identity. A staged identity grants no
authority to ordinary polling and cannot silently widen CBI39 to an active-or-staged key set.

Property: before confirmation, every ordinary distribution attempt still authenticates only the
active endpoint.

### C5 — confirmation proves possession and usable distribution behavior

Confirmation requires a staged successor and makes exactly one ordinary bounded CBI39 attempt pinned
only to that successor. Only `policy-distribution-current` or `policy-distribution-applied` confirms
the transition. Endpoint authentication, binding, freshness, transport, cancellation, timeout, and
native registry refusals preserve the active pin and staged transition.

Property: no successor becomes active without one complete successful CBI39 synchronization under it.

### C6 — activation follows successful synchronization durably

After CBI39 succeeds, the anchor atomically advances to the staged generation and endpoint and clears
the stage. If anchor publication fails after the policy synchronization, the result exposes both the
successful distribution outcome and `endpoint-rotation-write-failed`; the durable stage remains so a
fresh attempt can confirm against the new cursor.

Property: every reported rotation success is durable, active, and has no staged successor.

### C7 — recovery is bounded and floor-aware

The anchor record is size-bounded, integrity-checked, and strictly decoded. Missing or corrupt state,
initial-identity mismatch, a generation below a supplied recovery floor, or an equal-generation
identity conflict fails closed. A returned floor describes the durable active generation only and
never advances for staging.

Property: opening never returns an active endpoint older than or conflicting with the supplied floor.

### C8 — both roots execute one shared rotation model

Reference C# and Minimal F# independently execute shared vectors covering valid staging, generation
gap, predecessor mismatch, self-rotation, invalid signatures, and mismatched predecessor key
material. Each root additionally covers bootstrap, idempotent and competing staging, confirmation
failure and success, recovery, rollback, and corruption while signing native P-256 evidence and
confirming through its native CBI39 client.

Property: every shared vector produces the same portable refusal or staging code in both roots.

## Deliberate limits

CBI56 rotates only the CBI39 response-authentication key. It does not rotate the CBI37 authority that
signs publisher-trust policy updates, and it does not alter the HTTPS URI or trust the successor on
announcement alone. The recovery floor detects rollback only when retained by a custodian outside
the anchor being opened; privileged custody remains a separate deployment boundary.
