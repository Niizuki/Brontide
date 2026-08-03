# CBI31 capability contract — verified local provider activation

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI31 is the first physical-distribution slice. It activates one already-present local provider
executable only after verifying its immutable digest and applying an explicit launch policy. The
host owns the resulting operating-system process and supplies its negotiated Portable Binding
conversation to the existing CBI30 activation path.

It is not a package format, downloader, installer, trust service, sandbox, retention policy, or
cross-domain identity protocol. "Dedicated process" means process separation with shell execution
disabled and standard streams redirected; it does not claim an operating-system security sandbox.

## Capabilities

### C1 — acquisition verifies an immutable local artifact

The local source must name an existing regular file and its expected SHA-256 digest. A missing file
is `artifact-unavailable`; a digest mismatch is `artifact-integrity-failed`. Neither outcome starts
a process.

### C2 — launch policy is explicit and precedes execution

The verified executable must be contained by the policy's allowed root and its argument vector must
equal the policy's allowed argument vector. A refusal is `launch-policy-refused` and starts no
process. Path containment is structural, not a string-prefix comparison.

### C3 — launch isolation is observable and bounded

An admitted artifact starts in a dedicated child process with shell execution disabled, no window,
and standard input, output, and error redirected. The resulting owner reports
`dedicated-process`; a start failure is `provider-process-start-failed` without leaking a runtime
exception as the contract outcome.

### C4 — the owner composes with CBI30 and owns retirement cleanup

The owner exposes the existing negotiated Portable Binding conversation. Successful Component
activation still observes the CBI30 realization, Release, retirement, and provider exit. Disposing
an unfinished owner closes the conversation and terminates the child process tree.

### C5 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI31 vectors and report the same code,
launch state, isolation observation, activation result, Release, retirement, and exit state.

## Phase-wide properties

- Acquisition and policy refusal are effect-free with respect to provider execution.
- Integrity is checked from file bytes, using an ordinal hexadecimal digest comparison.
- Launch never uses a command shell or a caller-controlled working-directory search.
- No successful activation bypasses CM4 Release or the existing portable retirement exchange.
- Failures remain explicit data; cleanup remains idempotent.
