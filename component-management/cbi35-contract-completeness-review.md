# CBI35 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI35 publisher-trust contract, separate from its conformance tests.

## Findings and dispositions

1. **Entry order could change a policy identity.** Disposition: closed. Identity encoding sorts by
   exact publisher-key identity and pins a neutral golden digest shared by both roots.
2. **Duplicate or empty policy snapshots are ambiguous.** Disposition: prevented. Both are invalid,
   as is an identity that does not cover the complete snapshot; no invalid policy authorizes.
3. **Revocation could collapse into absence.** Disposition: prevented. Admitted, revoked, and unknown
   keys produce distinct portable observations, and only admitted produces an authorization.
4. **Cryptographic validity could be mistaken for trust.** Disposition: prevented structurally. The
   evaluator accepts only the detached CBI34 verified value and reports evidence and trust separately.
5. **Trust could be mistaken for installation permission.** Disposition: prevented. Evaluation has
   no source, store, process, or acquisition surface and always reports `admission-not-attempted`.
6. **An authorization could be reused for different content.** Disposition: prevented by value shape.
   It carries the exact policy, publisher key, content identity, and canonical payload digest.
7. **A caller in the same process can construct a public verified-value record.** Disposition:
   bounded. The API expresses a trusted composition-root flow, not an in-process security boundary;
   callers must obtain the value from CBI34 before evaluation.
8. **Policy provenance and mutation authority are absent.** Disposition: bounded. CBI35 consumes one
   immutable snapshot. It does not load, sign, persist, distribute, or authorize policy changes.
9. **Revocation has no time, expiry, or retroactive cancellation.** Disposition: bounded. A decision
   applies only to the selected snapshot and does not cancel earlier acquisitions or active leases.
10. **Key rotation and aliases are absent.** Disposition: intentional. Entries address exact CBI34
    SPKI digests; lineage, delegation, organization names, and equivalent-key normalization remain out.

## Result

The CBI35 contract is complete for deterministic host trust evaluation of one CBI34-verified key
against one immutable policy snapshot, without claiming policy provenance, freshness, or artifact
admission. The next physical-distribution slice should compose matching trusted authorization with
CBI33 acquisition before any source is opened, while keeping policy loading, networking, and durable
coordination outside that boundary.
