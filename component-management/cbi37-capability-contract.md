# CBI37 capability contract — authoritative publisher-trust policy updates

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI37 verifies signed CBI35 policy snapshots from one host-pinned policy authority, applies them as a
strict monotonic predecessor chain, and makes CBI36 acquisition consult the current snapshot before
source access. A newer policy therefore supersedes authorizations issued under an older snapshot.

This is not certificate-chain discovery, multi-authority consensus, network distribution, durable
storage, rollback recovery, transparency logging, wall-clock expiry, active-process termination, or
garbage collection. The host pins one exact authority SPKI digest out of band; the in-memory registry
does not claim that bootstrap choice is itself authenticated.

## Capabilities

### C1 — one pinned authority controls policy provenance

The registry is constructed with a strongly typed SHA-256 identity for the exact authority SPKI
bytes. Updates use only `ECDSA-P256-SHA256`; embedded SPKI must hash to the pin and import as exact
P-256 key material. Authority mismatch, malformed key material, or unsupported algorithms fail
closed without changing current state.

### C2 — signatures cover a canonical complete update payload

The versioned canonical payload covers the positive sequence, optional predecessor policy identity,
and complete canonical CBI35 policy identity. Invalid policy snapshots or signatures are refused.
A neutral golden payload digest pins both independent encoders.

### C3 — updates form one strict monotonic predecessor chain

Bootstrap requires sequence `1` and no predecessor. Every successor requires exactly current
sequence plus one and names the exact current policy identity. Gaps, replay, rollback, and forks are
distinct sequence or predecessor refusals and cannot replace current state.

### C4 — successful application publishes one issuer-controlled current snapshot

Only verified, chain-valid updates produce `policy-update-applied` and atomically replace current
state. The returned current snapshot has no public construction path and carries authority identity,
sequence, and the exact immutable policy snapshot. Every refusal preserves the previous snapshot.

### C5 — current policy supersedes outstanding acquisition authorization

The governed acquisition gate linearizes against the registry. Missing current policy is
`publisher-trust-policy-unavailable`; an authorization naming any non-current policy is
`publisher-authorization-superseded`. Both refuse before source identity or member access. An exact
current authorization delegates unchanged CBI36 behavior. An acquisition already admitted to the
gate completes under that snapshot before an update can become current.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume shared CBI37 vectors and report the same update
code, current sequence and policy identity, governed trust/transport/admission codes, source-access
count, and unchanged-state observation.

## Phase-wide properties

- No unpinned or invalid signature can publish a current policy snapshot.
- Every refused update preserves the complete previous current snapshot.
- Current sequence increases by exactly one on every state transition and never decreases.
- No missing or superseded authorization path observes the acquisition source.
- Every delegated acquisition is linearized against one exact current policy snapshot.
