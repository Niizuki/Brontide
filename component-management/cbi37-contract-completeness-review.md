# CBI37 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI37 authoritative policy-update contract, separate from conformance tests.

## Findings and dispositions

1. **An embedded signing key could nominate itself as authority.** Disposition: prevented. The host
   pins the exact SHA-256 SPKI identity independently; updates cannot change that pin.
2. **Algorithm labels could hide key or signature substitution.** Disposition: closed for the one
   supported algorithm. Verification requires the exact label, P-256 curve OID, SHA-256, exact SPKI
   consumption, and RFC 3279 DER signatures.
3. **Signing only the policy identity could omit update order.** Disposition: prevented. The golden
   canonical payload covers domain marker, sequence, predecessor presence and identity, and policy
   identity; the latter commits to every CBI35 entry.
4. **Replay, rollback, gaps, and forks could replace current state.** Disposition: prevented. The
   registry accepts bootstrap sequence one only, then exactly one successor naming current policy.
5. **A refused update could partially mutate current state.** Disposition: prevented and pinned.
   Verification and chain checks complete before one atomic replacement; refusals retain current.
6. **A newer revocation could leave old acquisition authorizations usable.** Disposition: closed for
   new acquisition. The governed gate rejects non-current policy identities before source access.
7. **An update can race an acquisition.** Disposition: defined. Registry application and governed
   acquisition are linearized by one lock. An acquisition that enters first completes under its
   current snapshot; the update becomes current afterward. This can delay updates behind slow CBI33
   sources and is not a cancellation mechanism.
8. **Applied policy state is lost on restart.** Disposition: bounded. The registry is process-local
   and has no durable anti-rollback storage, crash recovery, or trusted sequence checkpoint.
9. **Authority compromise and rotation are absent.** Disposition: bounded. There is one immutable
   pin, no threshold authority, successor authority, emergency recovery, certificate chain, or
   transparency log.
10. **Revocation does not terminate staged or active artifacts.** Disposition: intentional. CBI37
    gates future acquisition only; CBI32 leases and existing CBI31 processes retain their lifecycle.

## Result

The CBI37 contract is complete for process-local authoritative, monotonic publisher-trust policy
updates and current-policy acquisition gating. The next boundary should durably checkpoint the
verified authority, sequence, and policy identity with atomic crash recovery and rollback detection
before production remote policy distribution is claimed.
