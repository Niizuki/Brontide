# CBI39 capability contract — authenticated fresh policy distribution

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI39 performs one bounded asynchronous synchronization attempt against an injected remote source,
authenticates a challenge-bound response under one host-pinned P-256 distribution endpoint key,
checks its local policy cursor and short freshness window, and only then offers its optional single
CBI37 update to the durable CBI38 registry.

This is not an HTTP client, TLS profile, DNS policy, service discovery mechanism, retry scheduler,
push channel, certificate authority, endpoint- or policy-key rotation scheme, trusted clock, secure
recovery-floor store, transparency log, or availability guarantee.

## Capabilities

### C1 — one exact endpoint key authenticates the complete response

The host pins the SHA-256 SPKI identity of one ECDSA P-256 endpoint key. Its signature covers the
domain, challenge, current cursor, issue and expiry seconds, update presence, and canonical digest of
the complete optional update. Key substitution, malformed SPKI, unsupported algorithm, and signature
tampering fail before policy state changes.

### C2 — challenge and cursor bind each response to one request state

Each attempt creates a fresh 256-bit cryptographic challenge and sends the current CBI38 sequence and
policy identity. The signed response must echo all three exactly. Replayed responses and responses
prepared for another local policy state are refused.

### C3 — only a short current freshness window is accepted

Signed whole-second issue and expiry instants must contain the host-supplied current time, permit at
most 30 seconds of future issue skew, and span no more than 15 minutes. Expired, future, inverted,
overlong, and unrepresentable windows are `policy-distribution-stale`.

### C4 — each attempt has explicit work, size, and time bounds

Synchronization invokes the source exactly once, accepts exactly zero or one update, bounds response
text to 1 MiB and policy entries to 4096, and requires a positive caller timeout no greater than one
minute. Timeout, caller cancellation, transport failure, and malformed or oversized response are
distinct. CBI39 never retries.

### C5 — authenticated delivery cannot bypass native durable application

A current response without an update leaves state unchanged. An update is applied only through CBI38,
which reuses CBI37 signature and monotonic-chain verification and publishes its checkpoint before live
advancement. Native update or checkpoint failure codes and the resulting recovery floor are preserved.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI39 vectors and report the same code,
current sequence, floor sequence, and one-attempt observation for authentication, binding, freshness,
bounds, transport, timeout, and native-update cases.

## Phase-wide properties

- No unauthenticated, replayed, cross-cursor, stale, oversized, or malformed response changes policy.
- Authentication of distribution metadata never substitutes for CBI37 policy-authority verification.
- Every accepted update is durable before it is returned as applied.
- Every attempt makes at most one source call and at most one durable update call.
- A returned floor always describes the durable registry state visible in the result.
