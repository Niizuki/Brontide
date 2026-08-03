# CBI35 capability contract — host publisher-key trust policy

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI35 evaluates one CBI34-verified publisher key against an immutable host policy snapshot. The
policy explicitly admits or revokes exact publisher-key identities; absence is an unknown-key
refusal. Evaluation is deterministic and effect-free, and a trusted result remains separate from
artifact transport and CBI32 admission.

This is not a certificate chain, organization or Actor identity map, online trust service, key
discovery protocol, policy distribution format, secure storage, clock, expiry service, transparency
log, compromise detector, or automatic acquisition gate. Policy provenance and update authority are
host responsibilities outside this slice.

## Capabilities

### C1 — one canonical identity names an immutable policy snapshot

A policy contains a strongly typed SHA-256 identity and distinct publisher-key entries, each exactly
`admitted` or `revoked`. Its identity is the SHA-256 of a versioned, ordinal-key-ordered canonical
encoding. Empty policies, duplicate keys, malformed identities, unknown dispositions, and policy
identity mismatch are `publisher-trust-policy-invalid`. A neutral golden identity pins both roots.

### C2 — only verified CBI34 evidence can be evaluated

Evaluation requires a detached `VerifiedProviderPublisherEvidence` value. Missing evidence is
`publisher-evidence-not-verified`; no key lookup or authorization is produced. The evidence content
identity, payload digest, and publisher key identity are preserved in every successful lookup
observation.

### C3 — admitted, revoked, and unknown keys are distinct outcomes

An admitted exact key reports `publisher-trusted` and returns a detached
`TrustedProviderPublisherAuthorization`. A revoked exact key reports `publisher-key-revoked`; an
absent key reports `publisher-key-unknown`. Revocation is an explicit decision rather than absence,
and neither refusal returns authorization.

### C4 — trust does not become artifact admission

Every result reports `publisher-evidence-valid` for supplied verified evidence and
`admission-not-attempted`. Evaluation opens no acquisition source, writes no transaction or staged
content, and starts no process. Publisher key identity never becomes an Actor identity or a general
authority grant.

### C5 — explicit caller policy may compose trusted authorization with CBI33

The evaluator has no acquisition surface. A caller may require a trusted authorization, verify that
its content identity matches the requested acquisition, and then invoke CBI33. Revoked, unknown,
invalid-policy, and unverified paths cannot supply that authorization.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI35 vectors and report the same
policy identity, trust code, evidence code, publisher key identity, content identity, authorized
state, admission code, and effect-free observation.

## Phase-wide properties

- Every policy decision is a pure function of one snapshot and one verified evidence value.
- No malformed, duplicate, unknown, or revoked path returns trusted authorization.
- Revocation always remains distinguishable from an unknown key.
- Every authorization carries the exact policy, key, content, and payload identities evaluated.
- Trust evaluation alone never performs transport, staging, activation, or authority projection.
