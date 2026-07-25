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
