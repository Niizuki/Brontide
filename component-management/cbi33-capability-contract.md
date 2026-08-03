# CBI33 capability contract — attributable provider artifact acquisition

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI33 reads one declared provider artifact set from a named acquisition source into a bounded,
host-private transaction and submits the completed bytes to CBI32 staging. The acquisition source,
transport outcome, publisher-evidence outcome, and local admission outcome remain separate
observations.

This is not an HTTP client, repository protocol, archive format, retry scheduler, credential store,
signature verifier, publisher trust service, malware scanner, durable download manager, or sandbox.
The source is an injected stream provider; CBI33 does not infer publisher identity or trust from the
source name, successful delivery, or matching content.

## Capabilities

### C1 — a bounded declaration names source and complete content

The request names a strongly typed expected source, the CBI32 content identity, distinct safe
relative members with uppercase SHA-256 digests and non-negative byte lengths, the declared
executable and arguments, and a positive total-byte limit. The member lengths must not overflow and
their sum must not exceed the limit. Invalid declarations are `acquisition-invalid`, open no source
stream, and create no transaction or staged directory.

### C2 — acquisition admits exactly declared bounded streams

The supplied source identity must equal the expected identity before any member is opened. Members
are requested once in canonical path order and copied with bounded reads. Missing members are
`acquisition-member-unavailable`; short or overlong streams are `acquisition-length-mismatch`; read
failures are `acquisition-transport-failed`. Every transport failure leaves no acquisition residue,
does not invoke CBI32 admission, and starts no process.

### C3 — attribution is an observation, not publisher evidence

Every result reports the expected source identity and a transport code. CBI33 reports
`publisher-evidence-not-evaluated` for every path because it owns no publisher verifier. A matching
digest does not change that observation, and a source identity never becomes a publisher identity
or an authority grant.

### C4 — transport completion and local admission remain distinct

Reading every declared byte reports `transport-completed` even when CBI32 subsequently refuses the
content. Digest mismatch therefore reports transport completion separately from
`artifact-set-integrity-failed`; transport success is never presented as staging success. Only a
successful CBI32 result exposes a `StagedProviderArtifactSet`.

### C5 — successful acquisition composes with the complete CBI32 lifecycle

Successfully admitted bytes occupy the canonical CBI32 content address, remain usable after the
source is unavailable, and retain CBI32 reuse, inactive staging, CBI31 activation lease, retirement,
and exact-removal behavior. The private acquisition transaction is removed on every outcome.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI33 vectors and report the same
source identity, transport code, publisher-evidence code, admission code, staged state, reuse state,
activation result, removal result, and residue observation.

## Phase-wide properties

- Every result attributes the attempted source without treating that attribution as publisher
  evidence or authority.
- No failure leaves private acquisition content or a partially published content address.
- No source can cause more than the declared per-member length plus one probe byte, or exceed the
  declared total-byte limit, to be consumed.
- Transport completion alone never exposes an activatable artifact set.
- Every successful result is a valid CBI32 staged set and can use only the existing CBI32/CBI31
  activation path.
