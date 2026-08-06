# CBI58 capability contract — policy-authority rotation distribution

Date: 2026-08-06

Status: implementation contract

## Boundary

CBI57 verifies and durably retains a policy-authority rotation supplied by a caller. CBI58 supplies
that statement through one injected source call authenticated by the active CBI39 distribution
endpoint. The request and signed response bind a fresh challenge, the exact durable policy cursor,
and the exact active authority generation and identity. An accepted response contains zero or one
complete CBI57 statement and is routed only through the durable registry.

This is a separate single-attempt protocol so the established CBI39/CBI40 policy-update contract and
wire remain compatible. It is not polling, retry, endpoint discovery, authority transparency,
privileged floor custody, or remediation of a compromised predecessor.

## Capabilities

### C1 — every attempt names the exact durable authority cursor

The client creates one random 256-bit challenge and snapshots the current policy sequence and
identity plus active authority generation and identity before opening the source. The source is
called exactly once.

Property: no response for another policy or authority cursor can change the registry.

### C2 — the active distribution endpoint authenticates the whole response

The response uses `ECDSA-P256-SHA256`; its exact P-256 SPKI digest must equal the configured endpoint
identity, and its signature covers every request cursor field, freshness field, and the digest of the
optional complete CBI57 statement.

Property: changing any signed response field after signing prevents rotation.

### C3 — challenge, freshness, and concurrent movement fail closed

The challenge must match, issuance may be at most 30 seconds in the future, expiry must be after now
and issuance, and validity may be at most 15 minutes. The durable cursor is compared again after
authentication so a concurrent policy update or authority rotation yields `policy-authority-distribution-superseded`.

Property: no stale, replayed, or superseded response changes durable bytes.

### C4 — only CBI57 decides whether a delivered statement applies

No statement reports `policy-authority-distribution-current`. A statement is passed once to the
durable registry; its native CBI57 refusal code is preserved, and success is reported as
`policy-authority-distribution-applied` only after CBI57 has durably published it.

Property: every reported applied response is present in the durable CBI57 chain.

### C5 — transport, timeout, cancellation, and size are bounded

The client accepts a positive timeout of at most one minute, propagates caller cancellation, and
classifies timeout and source failures as data. Response text is bounded to 1 MiB and the source owns
any concrete framing or network policy.

Property: no classified pre-application failure changes registry state.

### C6 — both roots execute one shared distribution model

Reference C# and Minimal F# independently execute shared vectors for current, applied, endpoint
mismatch, invalid signature, challenge mismatch, cursor mismatch, stale response, and native CBI57
refusal. Each root also covers timeout, cancellation, source failure, supersession, and durable
recovery after application.

Property: every shared vector produces the same portable result code in both roots.

## Deliberate limits

CBI58 supplies one authenticated rotation statement to one host. It does not schedule attempts,
define a portable wire or HTTPS adapter, rotate the immutable authority pin, or establish privileged
custody for either authority floor. Those remain separate boundaries.
