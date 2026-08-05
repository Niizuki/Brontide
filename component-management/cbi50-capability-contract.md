# CBI50 capability contract — offline service enforcement

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI49 decides whether existing service may survive an unavailable publisher-policy endpoint, but it
performs no provider effects. CBI50 binds that decision to one exact, host-supplied serving set. It
keeps the set untouched only while CBI49 permits existing service and otherwise retires every
admitted member and terminates its concrete provider.

This is one bounded host call, not a daemon, timer, trust decision, restart controller, or staged
artifact collector. An availability stop is not publisher revocation: staged artifacts remain
available for a later restart policy to evaluate through the normal trust boundary.

## Capabilities

### C1 — one snapshot determines decision and effects

Enforcement accepts zero through 64 opaque serving activations, validates the whole set, and passes
that exact count to CBI49. Non-empty members must be serving and occurrence identities must be
unique. Invalid input is `offline-enforcement-invalid` before policy evaluation or effects.

Property: every non-invalid result's policy decision was made with the returned admitted count.

### C2 — permitted continuation is effect-free

`offline-existing-service` returns `offline-enforcement-continuing`; `offline-idle` over an empty set
returns `offline-enforcement-idle`. Neither retires a member, terminates a provider, or removes a
staged artifact.

Property: every result whose decision permits continuation has zero member observations and leaves
all admitted members serving.

### C3 — every stop decision reaches every member

`offline-grace-expired`, `offline-service-stop-required`, and `offline-observation-invalid` stop all
admitted members. Each member retirement is attempted and its concrete provider is terminated even
when graceful retirement fails.

Property: after a complete stop result, no admitted member remains serving.

### C4 — typed occurrence identity determines order

Members are processed and reported in ordinal `OccurrenceId` order, independent of caller order.
The coordinator neither accepts nor manufactures bare-string identities.

Property: every non-empty observation list is strictly ordered and contains each admitted identity
exactly once.

### C5 — one failure does not hide siblings

Retirement or provider-termination failure is recorded per member and does not prevent later members
from being attempted. The aggregate is `offline-enforcement-stopped` when every stop completes,
`offline-enforcement-cleanup-incomplete` when only graceful retirement is incomplete, and
`offline-enforcement-incomplete` when any concrete provider cannot be confirmed stopped.

Property: every admitted member receives one observation even after an earlier member failure.

### C6 — availability stop retains staged artifacts

CBI50 performs no staged-set removal. Offline availability says nothing about artifact integrity or
publisher authority, and retained bytes may be shared by another activation or reconsidered by the
separate restart policy.

Property: no CBI50 path invokes staged-artifact removal.

### C7 — preflight refusal has zero effect

Oversized, duplicate, null, or unavailable input returns `offline-enforcement-invalid`, origin
`preflight`, and an empty member list. No supplied serving member is retired or terminated.

Property: every preflight refusal has an empty observation list and preserves all otherwise-serving
members.

### C8 — both roots execute one shared enforcement model

Reference C# and Minimal F# independently execute the shared vectors and report the policy and
aggregate codes, origin, deterministic occurrence order, stopped count, serving state, and staged
artifact retention.

Property: every shared vector produces the same portable observation in both roots.

## Contract-completeness review

The contract states the zero-member case, the exact grace boundary, invalid observations, duplicate
and unavailable members, deterministic order, per-member failure isolation, and artifact ownership.
It deliberately does not state restart eligibility, retry timing after enforcement, process-tree
termination, concurrent mutation semantics, or durable recording of the stop. Those are not silent
CBI50 behavior: restart policy and durable/cross-process supervision remain later host boundaries.

## Deliberate limits

The call is sequential and caller-triggered. It does not acquire, launch, admit, or restart a
provider; manufacture trust evidence; remove staged content; schedule itself; own cross-process
leases; rotate endpoints or keys; or provide production isolation.
