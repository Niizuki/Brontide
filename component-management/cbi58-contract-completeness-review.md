# CBI58 contract-completeness review

Date: 2026-08-06

Status: complete

This review asks what the CBI58 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C6.

## Findings closed in the contract

- **Adding rotation to CBI39 would silently replace an established wire.** CBI40 rejects trailing
  bytes and has exact golden encodings, so appending an optional rotation would either break every
  existing peer or require ambiguous version negotiation. CBI58 is a separate typed attempt. It can
  gain its own wire later without weakening CBI39/40.
- **The policy cursor alone does not identify the rotation position.** A rotation does not advance
  policy sequence. The request and response therefore also bind active authority generation and
  identity, and the client compares all four cursor fields again after endpoint authentication.
- **Endpoint authentication must cover the CBI57 evidence, not merely announce it.** The endpoint
  signature includes a digest of every rotation field and both authority signatures. CBI57 then
  independently verifies those authority signatures; delivery authority and policy authority remain
  distinct.
- **No-update is a useful authenticated answer.** A source may report the authority cursor current
  without inventing a rotation. That response still has the same challenge, freshness, endpoint,
  and concurrency checks as a delivered statement.
- **Delivery must not reinterpret native refusal.** A valid endpoint can deliver a stale or otherwise
  invalid CBI57 statement. The durable registry remains the only rotation decision-maker and its
  exact refusal code is returned.
- **The effect test had to observe recovery, not only a label.** Deliberately bypassing `Rotate` while
  returning an applied label made both shared suites fail at generation zero. The final shared vector
  reopens the checkpoint with the returned authority floor before reporting the generation.

## Residual limits

CBI58 has an injected source rather than portable framing or a concrete HTTPS adapter. It performs
one attempt and does not schedule or retry it. Rotation of the immutable pin, predecessor-compromise
remediation, transparency, and custody of the externally retained authority floor remain deployment
boundaries. The next bounded implementation boundary is a strict portable CBI58 wire and concrete
single-attempt HTTPS source, without merging it into CBI40.
