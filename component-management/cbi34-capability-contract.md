# CBI34 capability contract — provider publisher evidence verification

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI34 verifies detached publisher evidence over one canonical CBI33 acquisition manifest. The
evidence binds the complete CBI32 content declaration and CBI33 member lengths to possession of one
ECDSA P-256 private key. Verification is effect-free and reports cryptographic validity separately
from host trust, transport, and local admission.

This is not a certificate format, certificate-authority client, key registry, trust store, revocation
service, transparency log, timestamp authority, package repository, transport, acquisition policy,
or installation grant. A valid signature proves only that the corresponding private key signed the
canonical bytes; it does not prove who controls that key or whether the host should trust it.

## Capabilities

### C1 — one canonical payload covers the complete acquisition manifest

The signed payload uses a version marker and length-prefixed binary fields for the CBI32 content
identity, members in ordinal relative-path order with digest and exact length, executable path, and
argument vector. Source identity and total-byte limit are host transport policy and are not signed.
Invalid acquisition declarations are `publisher-evidence-request-invalid`. A neutral golden payload
digest pins the encoding across both roots.

### C2 — evidence has one explicit key identity and algorithm

Evidence names `ECDSA-P256-SHA256`, an uppercase SHA-256 key identity, a base64 SubjectPublicKeyInfo
P-256 public key, and a base64 RFC 3279 DER signature. The key identity must equal the digest of the
exact public-key encoding. Missing evidence is `publisher-evidence-not-provided`; malformed fields,
wrong curves, and key-identity mismatch are `publisher-evidence-malformed`; any other algorithm is
`publisher-evidence-unsupported`.

### C3 — verification binds the exact canonical payload

A valid signature reports `publisher-evidence-valid` and returns a detached verified value naming
the content identity, publisher key identity, and payload digest. Changed paths, digests, lengths,
executable metadata, arguments, signature bytes, or signing key report `publisher-evidence-invalid`
and return no verified value.

### C4 — cryptographic validity is not trust or admission

Every result reports `publisher-trust-not-evaluated` and `admission-not-attempted`. Verification
opens no acquisition source, writes no transaction or staged content, starts no process, and does
not convert a valid key into authority.

### C5 — explicit caller policy may compose valid evidence with CBI33

The verifier itself has no acquisition surface. A caller may inspect a valid result, apply policy
outside CBI34, and then invoke CBI33. The resulting transport and CBI32 admission remain their own
observations. An invalid result cannot supply a `VerifiedProviderPublisherEvidence` value.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI34 vectors and report the same
payload digest, evidence code, publisher key identity, verified state, trust code, admission code,
and effect-free observation.

## Phase-wide properties

- Every evidence result is effect-free and leaves trust, transport, and admission undecided.
- No canonical manifest field covered by C1 can change without invalidating existing evidence.
- No malformed, unsupported, mismatched-key, or invalid-signature path returns a verified value.
- A verified value always carries the recomputed public-key identity and canonical payload digest.
- Publisher key identity never becomes Actor identity, source identity, or an authority grant.
