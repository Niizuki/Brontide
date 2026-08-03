# CBI40 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI40 portable wire codec and HTTPS source, separate from conformance
tests.

## Findings and dispositions

1. **The two stacks could serialize the same envelope differently.** Disposition: prevented and
   pinned. Version markers, big-endian integers, strict UTF-8 length prefixes, presence markers,
   sorted entries, exact EOF, and shared golden SHA-256 images define one byte representation.
2. **The wire could omit security-relevant response fields.** Disposition: prevented. It preserves
   challenge, cursor, issue/expiry instants, optional complete CBI37 update, endpoint algorithm,
   endpoint SPKI, and endpoint signature.
3. **Malformed lengths or counts could consume unbounded work.** Disposition: bounded. Messages and
   strings are limited to 1 MiB, policy entries to 4096, lengths are checked before slicing, and
   malformed identities or dispositions fail closed.
4. **A partial or extended image could be interpreted.** Disposition: prevented. Truncation, invalid
   UTF-8, unknown markers/presence values, and every trailing byte are refused.
5. **The adapter could silently downgrade to cleartext or another endpoint.** Disposition: prevented
   at the adapter seam. Only an absolute HTTPS URI is accepted and the final response URI must equal
   it. TLS certificate validation and redirect behavior inside the injected handler remain host-owned.
6. **HTTP metadata could change representation semantics.** Disposition: prevented. Status must be
   200, media type exact and parameter-free, and content encoding absent; compression is not accepted.
7. **A false or absent content length could bypass the body bound.** Disposition: prevented. Declared
   oversize is rejected early and every streamed read is counted independently against 1 MiB.
8. **Transport could retry or conceal cancellation.** Disposition: prevented locally. There is one
   `SendAsync`; the caller token reaches send, request content, stream creation, and reads. An injected
   handler may have its own behavior, which CBI40 neither configures nor claims.
9. **Successful HTTPS could be mistaken for policy authority.** Disposition: prevented by composition.
   The decoded response still crosses CBI39 endpoint signature/freshness, CBI37 update authority and
   monotonicity, and CBI38 publication-before-advancement.
10. **Operational distribution remains absent.** Disposition: explicit. Poll scheduling, bounded
    retry/backoff, jitter, offline behavior, endpoint discovery/rotation, observability, and service
    implementation are not part of CBI40.

## Result

The CBI40 contract is complete for portable bounded wire framing and one concrete cancellable HTTPS
attempt into CBI39. The next boundary should define a host-owned polling scheduler with bounded
retry/backoff and durable success-floor handoff; endpoint/key rotation and platform anchors remain
separate security work.
