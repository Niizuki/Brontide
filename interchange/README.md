# Cooling interchange fixture 0.1

This directory is the neutral, data-only contract for the first Reference/Minimal interchange proof.
It contains no CLR project, generated runtime type, shared exception, or dependency-injection
registration. Brontide Reference Stack and Brontide Minimal Stack parse the same files into independently implemented binding types.

The test protocol is version 2. Each UTF-8 JSON-lines message is one complete JSON object on one
line. Standard input and output carry protocol messages; standard error is diagnostic only.
Version 1 remains Brontide Minimal Stack's historical manifest/value seam and is not upgraded in place.

The authored contract is `interchange.tests.cooling.set-enabled` version 1. Its input and output
Shapes have independent identities. The input is open to authored Fragments and requires
`interchange.tests.cooling.host-context` version 1. Hosts construct that Fragment locally before
authority evaluation. A provider receives accepted invocation data, never a Capability.

`manifest-v2.json` is the golden descriptor. `values/` and `messages/` contain positive and
fail-closed fixtures. `contract-matrix.md` records the native mappings and the one deliberate
semantic Adapter on the Brontide Reference Stack side.

Run the complete clean interchange gate from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

The protocol and descriptor are experimental test instruments. Passing these fixtures does not
ratify a Brontide Portable Binding.
