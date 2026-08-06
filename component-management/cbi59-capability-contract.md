# CBI59 capability contract — policy-authority rotation wire

Date: 2026-08-06

Status: implementation contract

## Boundary

CBI58 defines one authenticated, fresh, single-attempt source seam for delivering zero or one CBI57
authority rotation. CBI59 gives that seam its own canonical binary request/response representation and
one concrete HTTPS source. It remains distinct from the unchanged CBI39/CBI40 policy-update wire.

This is portable framing and one injected-`HttpClient` adapter. It is not polling, retry, TLS/DNS or
proxy configuration, redirect policy, endpoint discovery or rotation, authority-pin rotation, or
privileged floor custody.

## Capabilities

### C1 — request and response have one canonical portable encoding

The codec uses strict UTF-8, signed big-endian 32-bit lengths/presence tags, signed big-endian 64-bit
integers, fixed `CBI59-REQUEST` and `CBI59-RESPONSE` markers, and a declared field order. Optional
policy identity and rotation use only presence tags zero and one. Both roots reproduce shared exact
SHA-256 golden encodings.

Property: equal typed messages always produce byte-identical encodings in both roots.

### C2 — decoding is strict, total, and bounded

Messages are non-empty and at most 1 MiB. Strings use strict UTF-8 and bounded lengths; identities
use their native validated types; presence tags are exact; a rotation contains every CBI57 field;
and trailing, truncated, malformed, or unknown data is refused as invalid wire data.

Property: no byte sequence yields a value unless the complete input is consumed exactly once.

### C3 — the concrete source makes one exact HTTPS request

Construction requires one absolute HTTPS URI without user information or fragment. `FetchAsync`
sends exactly one POST to that URI with exact `Content-Type` and `Accept`
`application/vnd.brontide.cbi59`, no content encoding, and the canonical request body. Only status
200 from the exact configured URI with that exact unparameterized response media type is accepted.

Property: no redirect, alternate endpoint, status, media type, parameter, or content encoding can
produce a typed response.

### C4 — declared and streamed response size are independently bounded

A declared `Content-Length` above 1 MiB is refused before reading. The source also counts bytes while
streaming and refuses the first byte beyond 1 MiB regardless of missing or false length metadata.

Property: the decoder is never invoked with more than 1 MiB.

### C5 — cancellation propagates and the adapter never retries

Caller cancellation reaches send, response-stream acquisition, and every read. Each `FetchAsync`
call invokes the injected handler exactly once and preserves transport, cancellation, and malformed
wire failures for CBI58 to classify.

Property: every source call issues at most one HTTP request.

### C6 — both roots compose the wire through CBI58 and durable CBI57

Reference C# and Minimal F# independently execute the shared golden current and rotation messages,
strict-decode mutations, HTTP metadata and size refusals, cancellation, and an end-to-end response
that CBI58 endpoint-authenticates and durably applies through CBI57.

Property: every shared message has the same digest and typed observation in both roots.

## Deliberate limits

CBI59 owns framing and one exact single-attempt HTTPS adapter only. The host still configures the
`HttpClient` handler, certificates, DNS, proxy, connection pooling, timeout, and redirect behavior;
CBI58 owns attempt timeout and authentication. Scheduling and retry remain a later boundary.
