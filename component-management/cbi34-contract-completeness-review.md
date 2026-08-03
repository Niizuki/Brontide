# CBI34 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI34 publisher-evidence contract, separate from its conformance tests.

## Findings and dispositions

1. **Signing only the CBI32 digest would omit CBI33 lengths.** Disposition: closed. The payload
   includes the content identity and repeats every ordered path/digest together with its exact byte
   length, executable designation, and argument vector. A golden payload digest pins the encoding.
2. **Host transport policy can be confused with publisher content.** Disposition: prevented. Source
   identity and total-byte limit are deliberately excluded. Changing either leaves the payload
   unchanged, while changing any content field invalidates existing evidence.
3. **Caller-owned mutable Reference collections can change between validation and encoding.**
   Disposition: corrected and pinned. A switching `IReadOnlyList` made canonical encoding reject a
   declaration that was valid on entry. Reference now snapshots files and arguments before either
   validation or encoding; Minimal lists are immutable.
4. **A public key embedded beside its signature does not authenticate a publisher name.**
   Disposition: bounded. The publisher key identity is the SHA-256 digest of the exact SPKI bytes.
   CBI34 proves possession of that key only and exposes no person, organization, Actor, or authority
   mapping.
5. **Algorithm labels can hide curve or signature-format substitution.** Disposition: closed for
   the one supported algorithm. Verification requires the exact algorithm label, P-256 curve OID,
   SHA-256, and RFC 3279 DER signature format. Wrong curves and trailing SPKI bytes are malformed.
6. **Equivalent key encodings can produce different identities.** Disposition: intentional. Key
   identity addresses exact SPKI evidence bytes, not an abstract elliptic-curve point. A registry
   may later normalize or alias keys, but CBI34 does not.
7. **ECDSA signatures are not content identifiers.** Disposition: prevented structurally. The
   signature bytes never enter CBI32 identity or the verified-value identity. Verification returns
   the canonical payload digest and recomputed key identity.
8. **Evidence can be replayed.** Disposition: bounded. CBI34 has no issuance time, expiry, nonce,
   transparency log, or freshness claim. Identical canonical content signed by the same key remains
   valid; replay policy belongs to a later trust boundary.
9. **Key compromise and revocation are invisible.** Disposition: bounded. There is no trusted key
   registry, rotation lineage, revocation source, or policy time. Cryptographic validity remains
   `publisher-trust-not-evaluated` on every path.
10. **Valid evidence can be mistaken for installation permission.** Disposition: prevented. The
    verifier has no source, store, process, or acquisition method and always reports
    `admission-not-attempted`. CBI33 composition is an explicit caller action after verification.
11. **Malformed evidence must not become a verified value through exception handling.** Disposition:
    closed. Invalid base64, key imports, key identifiers, curves, and signatures fail closed; only
    the valid path constructs `VerifiedProviderPublisherEvidence`.

## Result

The CBI34 contract is complete for detached ECDSA P-256 publisher-key evidence over the canonical
CBI33 manifest, without claiming publisher identity, freshness, trust, or installation authority.
The next physical distribution slice should define host trust policy over verified publisher keys,
including explicit key admission and revocation observations, while keeping trust decisions
separate from cryptographic validity and artifact admission.
