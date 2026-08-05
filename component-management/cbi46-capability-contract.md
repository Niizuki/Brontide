# CBI46 capability contract — serving trust sweep

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI45 revalidates one opaque serving activation. CBI46 defines the host policy for one explicit
sweep over a supplied serving set. The host validates the whole set, orders it by typed occurrence
identity, and invokes CBI45 once for every member in that order.

This is one bounded call, not a resident service. It does not schedule itself, watch policy files,
run members concurrently, retry cleanup, restart providers, or rotate authority keys.

## Capabilities

### C1 — the serving set is bounded and valid before effects

A sweep accepts between one and 64 opaque CBI45 activations. Every activation must still be serving
and every occurrence must be unique. An empty, oversized, duplicate, or unavailable set is refused
as `serving-trust-sweep-invalid` before any member is revalidated or retired.

### C2 — typed occurrence identity determines order

The opaque activation retains the `OccurrenceId` used to create its portable lifecycle. Results are
ordered by that identity's ordinal token, independent of caller enumeration order. The sweep neither
accepts nor manufactures a bare-string member identity.

### C3 — every admitted member receives one current decision

After preflight, the sweep invokes CBI45 exactly once for every ordered activation. Each observation
retains CBI45's launch and serving policy identities, authorization, refusal origin, and cleanup code.
Under CBI38's one-writer bound, all decisions observe the same current policy. A concurrent writer is
outside this slice; per-member policy identities make any such race visible rather than claiming an
atomic snapshot.

### C4 — trust withdrawal reaches every affected member

Every publisher that the current policy revokes or omits is retired through CBI45. Its provider is
terminated and staged-set removal is attempted. One withdrawal does not stop the sweep from reaching
later members.

### C5 — one member's outcome does not hide its siblings

The sweep returns one observation per admitted occurrence even when an earlier member is withdrawn
or reports incomplete cleanup. The aggregate is `serving-trust-sweep-current` when all continue,
`serving-trust-sweep-withdrawn` when one or more stop with complete cleanup, and
`serving-trust-sweep-cleanup-incomplete` when any stopped member reports incomplete cleanup. If an
activation becomes unavailable after preflight, the sweep still returns every observation and uses
`serving-trust-sweep-incomplete`; it does not misreport that race as a trust withdrawal.

### C6 — preflight refusal has zero effect

An invalid set produces no member observations and performs no policy evaluation, retirement,
provider termination, or staged-set removal. The result states `preflight` as its refusal origin.

### C7 — both roots agree

Reference C# and Minimal F# independently execute the shared vectors and report the aggregate code,
origin, deterministic occurrence order, continued and withdrawn counts, and per-member CBI45 codes.

## Phase-wide properties

- Every successful sweep returns exactly one observation for every distinct input occurrence.
- Every successful sweep's observations are in strictly increasing ordinal occurrence order.
- Every observation is the unaltered semantic result of one CBI45 call over that activation.
- Every admitted activation is processed even if an earlier activation is withdrawn or cleanup is
  incomplete.
- Every preflight refusal has an empty observation list and leaves every supplied activation serving.

## Deliberate limits

The sweep is sequential and caller-triggered. It defines neither cadence nor a policy-update event
loop, parallel dispatch, retry, restart, group-atomic cutover, shared-artifact reference counting,
privileged recovery-floor custody, endpoint rotation, or authority-key rotation. A concurrent policy
writer can cause different members to name different serving policy identities; hosts that require a
single snapshot must serialize writers around this call under the existing CBI38 ownership bound.
