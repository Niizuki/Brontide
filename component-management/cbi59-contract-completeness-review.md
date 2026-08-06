# CBI59 contract-completeness review

Date: 2026-08-06

Status: complete

This review asks what the CBI59 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C6.

## Findings closed in the contract

- **CBI58 needs its own wire rather than an extension of CBI40.** CBI40 has strict trailing-byte
  rejection and stable golden encodings. Separate CBI59 request and response markers preserve that
  boundary and prevent policy updates from being mistaken for authority rotations.
- **Canonical bytes must cover the complete cursor and evidence.** The request carries the policy
  cursor and active authority cursor. The response carries those fields, freshness, zero or one
  complete CBI57 statement, and the CBI58 endpoint-authentication material in a fixed order.
- **Typed validation does not replace framing validation.** Strict UTF-8, exact presence tags,
  bounded lengths, complete consumption, and native identity construction jointly refuse malformed
  input before CBI58 sees a typed response.
- **Header limits alone do not bound an HTTP body.** The adapter checks a declared length and counts
  the actual stream independently, including responses with no declared length.
- **A concrete adapter must not quietly broaden endpoint authority.** It accepts only one POST to
  the configured absolute HTTPS URI and verifies the effective URI, status, media type, parameters,
  and content encoding before decoding.
- **The effect test must cross both existing boundaries.** The composed test endpoint-authenticates
  the decoded CBI59 response through CBI58, applies the contained transition only through CBI57,
  and observes the durable generation rather than trusting a transport label.

## Residual limits

CBI59 does not configure certificates, DNS, proxying, redirect behavior, handler lifetime, or
timeouts on the injected `HttpClient`. It neither discovers endpoints nor schedules or retries
attempts. Rotation of the immutable authority pin, predecessor-compromise remediation,
transparency, and custody of the externally retained authority floor remain deployment boundaries.
The next bounded implementation boundary is host-owned scheduling for CBI58/CBI59 attempts, with a
durable retry policy that cannot weaken the single-attempt adapter or the native CBI57 decision.
