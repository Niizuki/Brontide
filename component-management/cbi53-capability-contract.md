# CBI53 capability contract — durable provider restart attempt history

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI52 performs one in-process restart attempt and CBI51 consumes caller-supplied attempt history.
CBI53 makes that history durable. One host-local journal is bound to a typed occurrence, retained
staged-content identity, maximum attempt count, and retry delay. A restart is marked in-flight before
CBI52 runs; a returned result is committed atomically and becomes the history supplied to the next
CBI51 decision.

The journal cannot atomically commit provider-process and portable-lifecycle effects. An in-flight
image reopened after interruption is therefore indeterminate until the host explicitly retries or
abandons it. This slice has one owner and no cross-process fencing; ownership is the next supervision
boundary.

## Capabilities

### C1 — one journal names one bounded restart lineage

Establishing requires a distinct `ProviderRestartAttemptRunId`, typed occurrence, staged-content
identity, and CBI51 policy of one through eight attempts. Existing paths and mismatched run,
occurrence, or staged identities are refused without mutation.

Property: every accepted record belongs to the journal's one occurrence and staged identity.

### C2 — every accepted transition is atomic and integrity-checked

The bounded record is written to a private temporary sibling, flushed, and atomically replaced. Its
SHA-256 tag detects accidental corruption and truncation. Invalid phase, counts, order, timing,
identity, or trailing data refuses open; a failed write preserves the prior durable image.

Property: every observable snapshot is either the complete prior state or complete successor state.

### C3 — policy refusal has no journal or restart effect

The durable coordinator evaluates CBI51 from committed attempt count and last-attempt time before
starting an attempt. Waiting, exhausted, terminal-cause, trust, state, observation, and current-proof
refusals leave the exact journal bytes unchanged and never call CBI52.

Property: no non-ready CBI51 decision creates an attempt record or in-flight marker.

### C4 — in-flight state precedes restart effects

After a ready decision, the journal persists the zero-based attempt index and injected instant as
in-flight before invoking CBI52. Failure to persist stops before launch. A returned enforcement
observation is then committed with its code, origin, provider-started, lifecycle-reconstructed, and
completed facts.

Property: CBI52 is called only while the durable image already names that attempt as in-flight.

### C5 — committed failures drive delay and exhaustion

Every returned non-completed attempt advances the committed attempt count and supplies its instant
as `lastAttempt`. Another attempt is waiting until that instant plus the journal delay. Committing the
configured maximum failed attempt terminates as `durable-restart-exhausted`.

Property: no lineage invokes more attempts than its durable maximum.

### C6 — interrupted work is indeterminate until reconciled

Opening an in-flight journal returns `durable-restart-indeterminate` and performs no policy or restart
work. `Retry` records one interruption and retry, clears the in-flight marker, and makes the same
attempt index ready for a fresh current-cycle decision. `Abandon` records the interruption and
terminates the lineage.

Property: recovery never interprets a missing committed result as proof that no effect occurred.

### C7 — success and terminal recovery are idempotent

A completed CBI52 result commits `durable-restart-completed`; exhaustion or abandonment commits its
terminal code. Opening or advancing a terminal journal returns the same snapshot without evaluating
policy, launching a provider, or changing bytes.

Property: one durable lineage records at most one completed successor.

### C8 — both roots execute one shared history model

Reference C# and Minimal F# independently execute shared transition vectors and report code, phase,
ordered attempt observations, next attempt index, in-flight index, interruption count, and retry
count.

Property: every shared vector produces the same portable observation in both roots.

## Contract-completeness review

The contract covers identity binding, bounded history, delay, exhaustion, in-flight ordering,
write failure, corruption, interruption, retry, abandonment, success, and terminal idempotence. It
deliberately does not coordinate two journal objects or processes, recover a provider connection
after the host itself dies, prove that an interrupted effect occurred, rotate endpoints or keys,
clean exhausted staged content, or provide privileged custody. Those remain ownership, external
reconciliation, distribution, maintenance, and security boundaries.

## Deliberate limits

CBI53 is a host-local journal and coordinator, not a daemon, cross-process lock, fencing-token
protocol, exactly-once executor, provider upgrade mechanism, secure store, or production sandbox.
