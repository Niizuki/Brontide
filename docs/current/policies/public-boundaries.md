# Public boundary operability and threat assumptions

Status: implementation-owned operational contract for the Architecture 0.5 evidence baseline and
experimental interchange projects. New architectural decisions come from
[Architecture 0.7](../architecture/Brontide-Architecture-0.7.md). This document is not an architecture
revision.

## Base authority boundaries

Both stacks require an explicit initiator, target, Operation, and presented Capability before a
handler can run. Unknown identities, Shapes, Constraints, targets, and Operations fail closed.
Minimal execution audits and Reference provenance retain identifiers, decision/status, reason, and
trusted time; they do not retain the rejected command payload. Outcome/Event payloads are retained
only after their declared Shape validates. Applications that persist Events remain responsible for
field-level classification and retention policy.

Trusted time is supplied by the host. A sender-provided timestamp does not become authority time.
Cancellation is a host/transport concern: Core/Kernel transitions are synchronous or receive a
token explicitly and do not consult ambient cancellation or clocks.

## Experimental process bindings

| Boundary | Payload/depth | Time and cancellation | Cleanup | Replay and denial-of-service assumptions |
| --- | --- | --- | --- | --- |
| Cooling v2 JSON-lines | JSON depth 64; no byte limit is currently claimed | Host I/O timeout is 10 seconds in retained tests; Reference propagates cancellation; Minimal turns timeout into an explicit binding failure | Host kills the complete provider process tree if exchange does not shut down cleanly | Single invocation per process; no replay protection. Use only with a locally selected executable and bounded input until a byte limit exists. |
| Catalog v1 JSON-lines | 65,536 UTF-8 bytes per complete line; JSON depth 32; exact fields and variants | Host I/O timeout is 10 seconds in retained tests; timeout/cancellation terminates the provider process | Normal shutdown is acknowledged; abnormal completion kills the complete process tree | Request IDs cannot repeat within a provider process. Replay memory and resource data are ephemeral. The line limit bounds message allocation, not total process lifetime or item count across many messages. |

Standard output is protocol-only and standard error is diagnostic-only. Provider-private stack
traces, CLR type metadata, exception objects, Capabilities, service containers, and static state do
not cross either seam. Cooling explicitly rejects exception/type metadata; Catalog accepts only its
exact field sets. Provider selection and executable trust are host responsibilities.

Catalog resource references identify provider-owned sandbox state; they do not confer access.
Providers independently check the provider/id scope and return `resource-refused` before mutation.
No filesystem path, URI dereference, credential, or Capability is transported by this proof.

These bindings are local-process experiments, not hardened network services. They assume an
operating-system account and process launcher already trusted by the host. They do not claim
multi-tenant isolation, distributed replay protection, back-pressure across unbounded sessions,
cryptographic peer identity, or protection from a malicious executable selected by the host.

## Experimental portable binding

The two rows above describe the retained line-delimited JSON experiments. The Portable Component
Binding is a separate, later seam with its own declarations; the JSON-lines protocol is diagnostic
and legacy and is never the portable wire contract. This section records the portable seam's
operational assumptions. It is experimental evidence, not a ratified extension. Both stacks now
state Architecture 0.8 as their local implementation target; this boundary does not change that
target or ratify the architecture.

| Boundary | Payload/depth | Time and cancellation | Cleanup | Replay and denial-of-service assumptions |
| --- | --- | --- | --- | --- |
| Portable Component Binding 0.1 (length-delimited deterministic CBOR) | 65,536 bytes per frame behind a 4-byte big-endian length prefix; nesting depth 32; at most 256 record fields, 16 Fragments per record, 4,096 sequence items, 16,384 text bytes, 32,768 byte-string bytes, and 32,768 resource bytes. Every bound is declared in the Binding Plan and enforced before uncontrolled work. Unknown fields, unknown enumeration values, and undeclared control content are refused. | Declared I/O timeout of 10 seconds. A cancellation past the declared bound is classified `timeout`; an allocation failure is `resource-exhausted`; a disposed stream or I/O failure is `transport-unavailable`; anything else is `unknown` carrying why narrower attribution was impossible. | Withdrawal and termination are explicit lifecycle frames, and the states are checked at the endpoint rather than inferred from arrival order. The binding layer never starts a peer — it is handed an already-connected duplex — so process lifetime, launch, and kill remain the host harness's concern. | At most one concurrent request per binding scope. Replay protection is declared per scope and enforced on request identity inside the declared window. Decoders are property-tested within deterministic bounds against arbitrary bytes, single-byte mutations, truncations, nesting past the declared depth, and hostile length prefixes. Ordering, retry, cancellation, streaming, and exactly-once execution are explicit non-promises. |

No Capability crosses a trust boundary on this seam: the host refuses to emit authority-bearing
content before anything leaves it, and the provider's own domain performs its own admission. A local
authority denial is frameless — it starts no provider and emits nothing — so a denial is never
observable to the peer as a message. No private exception, stack trace, runtime type name, or
authority object is transported; a semantic failure crosses as a shaped Outcome instead.

Referenced resources are limited to a copied immutable blob verified by SHA-256 content hash and a
retained addressing-only handle that confers no access. Borrowed regions, transferred ownership, and
fallback policies are version 0.1 non-goals that fail negotiation closed rather than degrading.

This seam makes the same trust assumptions as the experiments above: a locally selected executable,
an already-trusted account and launcher, no cryptographic peer identity, and no claim of
multi-tenant isolation or protection against a hostile provider chosen by the host.
