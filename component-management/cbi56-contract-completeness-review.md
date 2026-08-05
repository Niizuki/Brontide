# CBI56 contract-completeness review

Date: 2026-08-05

Status: complete

This review asks what the CBI56 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to the written C1-C8 requirements.

## Findings closed in the contract

- A signed successor announcement proves authorization by the current endpoint, not possession of
  the successor private key or usable distribution behavior. C5 therefore requires a complete CBI39
  synchronization under the staged key before activation.
- Trusting active and staged keys together would silently widen the authentication boundary and make
  a staged key authoritative before proof. C4 keeps ordinary polling pinned only to the active key.
- A crash between announcement and confirmation must not erase the operator-visible transition. C3
  makes the single staged successor durable before any successor attempt.
- CBI39 can update policy before publishing the endpoint anchor fails. C6 exposes both outcomes and
  preserves the staged transition, rather than claiming the entire operation had no effect.
- A staged successor is not an active rollback floor. C7 advances the returned floor only with the
  durable active generation and requires external custody for rollback detection.
- The distribution endpoint authenticates CBI39 transport responses but does not sign CBI37 policy.
  The boundary and deliberate limits keep endpoint rotation separate from policy-authority rotation.

## Residual limits

The contract intentionally does not solve endpoint URI discovery, certificate/TLS policy,
multi-endpoint failover, transparency, quorum, secure clocks, privileged floor custody, or hostile
storage rollback. Policy-authority-key rotation remains the next integration/security slice.
