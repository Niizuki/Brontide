# CBI40 capability contract — portable policy-distribution wire

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI40 gives CBI39 one portable, bounded, versioned binary request/response representation and one
concrete HTTPS `HttpClient` source that sends exactly one request, validates exact response metadata,
streams at most 1 MiB, decodes strictly, and returns the typed envelope to CBI39 authentication.

This is not a server, retry scheduler, polling service, redirect policy, DNS or proxy policy, custom
TLS trust policy, certificate pin, compression profile, endpoint discovery or rotation scheme,
trusted clock, availability guarantee, or replacement for CBI39 response authentication.

## Capabilities

### C1 — the request wire image is canonical, complete, and bounded

The versioned big-endian binary request contains the exact 256-bit uppercase challenge and a valid
sequence/policy-identity cursor. Strict UTF-8, explicit presence markers, 1 MiB total size, and exact
EOF produce one portable encoding. Shared golden SHA-256 evidence pins the byte image.

### C2 — the response preserves the complete optional update

The response carries every CBI39 envelope field and, when present, every CBI37 update field including
the canonical policy entries, authority SPKI, and signature. Entry order is canonical by publisher
key, count is limited to 4096, and shared golden digests pin current and update response images.

### C3 — malformed wire images fail closed

Unknown markers, invalid presence or count values, invalid UTF-8, invalid typed identities, oversized
strings or messages, truncation, and trailing bytes are refused before a typed response exists.

### C4 — the concrete adapter uses one exact HTTPS endpoint and media type

Construction requires one absolute HTTPS URI without user information or a fragment. The adapter
sends exactly one POST to it with exact CBI40 `Content-Type` and `Accept`. Only status 200 from the
same final URI with the same parameter-free media type and no content encoding is accepted.

### C5 — response streaming is bounded independently of metadata

Declared oversize is refused before body reading. Missing or dishonest `Content-Length` cannot bypass
the 1 MiB streaming counter. Caller cancellation reaches send and body reads, no retry occurs, and all
HTTP, stream, or wire failures remain CBI39 transport failure rather than trusted policy evidence.

### C6 — the wire composes with authenticated durable distribution in both roots

Reference C# and Minimal F# independently consume the shared vectors and golden digests. In each root,
a mocked HTTPS response crosses the real codec and adapter, passes CBI39 endpoint authentication and
freshness, passes CBI37 authority verification, and becomes a durable CBI38 checkpoint in one call.

## Phase-wide properties

- The two roots emit identical bytes for every shared golden wire value.
- No partial, ambiguous, compressed, redirected, oversized, or trailing response reaches CBI39.
- The adapter performs exactly one HTTP send and owns neither `HttpClient` nor its handler policy.
- Successful transport is not publisher policy authority; CBI39, CBI37, and CBI38 remain mandatory.
- Cancellation and every transport refusal leave the durable registry untouched.
