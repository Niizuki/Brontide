# Experimental interchange fixtures

This directory contains the neutral, data-only contracts for two Reference/Minimal interchange
proofs. It contains no CLR project, generated runtime type, shared exception, or dependency-injection
registration. Brontide Reference Stack and Brontide Minimal Stack parse the same files into independently implemented binding types.

These fixtures are narrow Architecture 0.5-era experimental evidence. Current design is sourced
from [Architecture 0.7](../docs/current/architecture/Brontide-Architecture-0.7.md), but these fixtures do not establish 0.7
conformance or a general portable binding protocol.

## Cooling proof

The Cooling test protocol is version 2. Each UTF-8 JSON-lines message is one complete JSON object on
one line. Standard input and output carry protocol messages; standard error is diagnostic only.
Version 1 remains Brontide Minimal Stack's historical manifest/value seam and is not upgraded in place.

The authored contract is `interchange.tests.cooling.set-enabled` version 1. Its input and output
Shapes have independent identities. The input is open to authored Fragments and requires
`interchange.tests.cooling.host-context` version 1. Hosts construct that Fragment locally before
authority evaluation. A provider receives accepted invocation data, never a Capability.

`manifest-v2.json` is the golden descriptor. `values/` and `messages/` contain positive and
fail-closed fixtures. `contract-matrix.md` records the native mappings and the one deliberate
semantic Adapter on the Brontide Reference Stack side.

Cooling is a single Boolean Operation with three record Shapes, one required Fragment, and one
invocation per provider process. It has a 64-level JSON depth bound and a host-configured 10-second
per-I/O timeout, but it does not yet impose a byte-size limit or replay window. It transfers no
Capability and dereferences no external resource.

## Catalog/resource proof

[`catalog/manifest-v1.json`](./catalog/manifest-v1.json) describes a separate version 1 protocol.
It carries a batch of nested items with repeated tags, invokes `upsert-items` and `find-items` in one
provider process, returns shaped `missing-items` failures, and crosses a provider-scoped resource
handle. Providers accept only `catalog-sandbox/shared`; another handle returns `resource-refused`
without mutation. A resource handle conveys addressing only, never authority.

Catalog messages are strict, single-line UTF-8 JSON with a maximum of 65,536 encoded bytes and a
maximum depth of 32. Unknown fields, unknown message/Operation variants, malformed JSON, protocol
version skew, and repeated request IDs fail visibly. Replay memory is process-local and lasts only
for that provider session. [`catalog/vectors`](./catalog/vectors) contains the neutral adversarial
inputs; both stacks run them through independently implemented parsers and endpoints.

[`binding-measurements.json`](./binding-measurements.json) records physical source lines and the
generated/manual split. `build/verify-binding-measurements.ps1` recomputes every file count. Both
implementations currently use zero generated lines; every measured binding line is manual and
stack-owned. Build timing is intentionally method-recorded rather than CI-gated because SDK startup
and cache state dominate the incremental observation.

Run the complete clean interchange gate from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

Both protocols and descriptors are experimental test instruments. Passing these fixtures does not
ratify a Brontide Portable Binding, demonstrate capability federation, or establish a machine or
network boundary.
