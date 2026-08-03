# CBI32 capability contract — content-addressed provider staging

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI32 stages one declared local multi-file provider artifact set into a host-owned,
content-addressed directory. Staging is inactive and effect-free with respect to provider execution.
An explicit activation lease may then launch the staged executable through CBI31; removal is legal
only after every lease has ended.

This is not a network acquisition protocol, archive or package format, signature verifier,
machine-wide installer, security sandbox, retention scheduler, garbage collector, or Dataset
lifecycle. The source and staging root are host-controlled local directories.

## Capabilities

### C1 — one canonical manifest names the complete artifact set

The declaration contains a content identity, source root, distinct safe relative file paths and
uppercase SHA-256 digests, one executable path that is a declared member, and a parsed argument
vector. The identity is the SHA-256 of the canonical ordered manifest. Empty sets, duplicate or
traversing paths, undeclared executables, malformed digests, and identity mismatch are
`artifact-set-invalid` and create no staged directory.

### C2 — staging is verified and transactional

Every source member must exist as a regular file and match its declared digest while being copied to
a private temporary directory. Missing members report `artifact-set-unavailable`; changed bytes
report `artifact-set-integrity-failed`. A failed attempt leaves neither a final content directory nor
temporary residue and never starts a process.

### C3 — content identity determines reuse and detects staged corruption

A successful set occupies exactly `<store>/<content identity>` and contains only declared members.
Restaging the same verified manifest reuses that directory. Existing staged content is reverified;
missing, additional, or changed members report `staged-artifact-integrity-failed` rather than being
silently replaced.

### C4 — staging remains inactive and composes with CBI31 activation

Successful staging starts no process and remains usable after the source disappears. Explicit
activation launches the declared staged executable through CBI31's digest, allowed-root, exact
arguments, dedicated-process, Release, retirement, and cleanup contract.

### C5 — removal respects active leases and exact ownership

Removal of an active set is `artifact-set-in-use` and preserves every file. After the owner is
disposed, removal deletes only that content-addressed directory and reports `removed`; an absent
identity reports `artifact-set-not-staged`. Source content and sibling staged identities are never
removed.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI32 vectors and report the same stage
code, staged state, reuse state, activation result, Release, retirement, provider exit, removal code,
and residue observation.

## Phase-wide properties

- Every staging failure is process-effect-free and leaves no partial content-addressed state.
- Every successful staged directory contains exactly the declared paths with the declared bytes.
- Staging, reuse, activation, and removal never mutate the source tree.
- No active lease can lose any staged member through the store's removal surface.
- Activation never bypasses CBI31 policy or CM4 Release.
