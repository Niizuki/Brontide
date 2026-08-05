# CBI48 capability contract — durable provider-trust cadence resumption

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI47 executes a bounded provider-trust cadence in one process. CBI48 gives that cadence a durable,
host-local journal and an explicit recovery protocol. It records each effectful cycle as in-flight
before invoking it, commits each returned observation atomically, resumes only at a clean boundary,
and refuses to guess whether an interrupted cycle took effect.

This is not exactly-once execution. CBI41 can publish policy and retain a floor, and CBI46 can retire
providers and attempt cleanup, before a process dies. No local journal can atomically commit those
independent effects with its own cursor. An in-flight recovery is therefore indeterminate until the
host reconciles the external state and explicitly chooses retry or abandonment.

The journal uses an ordinary integrity-tagged file. Its SHA-256 tag detects accidental corruption and
truncation, not an adversary who can rewrite the file and recompute the tag. CBI48 is not a daemon,
cross-process lock, offline policy, provider restart policy, endpoint/key rotation mechanism,
privileged floor custodian, or production sandbox.

## Capabilities

### C1 — a durable run is bounded and distinctly identified

A run has a dedicated `ProviderTrustCadenceRunId`, a CBI47 schedule, and a start instant. Establishing
creates exactly one journal for 1-64 cycles. Establishing over an existing path refuses without
changing it; opening under a different run identity refuses.

### C2 — every accepted transition is atomic and integrity-checked

The complete bounded journal is written to a private temporary sibling, flushed, and moved into
place. Opening refuses a mismatched tag, truncation, trailing data, invalid marker, impossible count,
or inconsistent phase. A refused transition leaves the prior bytes unchanged.

### C3 — in-flight state precedes every effectful cycle

Before a cycle delegate is called, the journal durably records its zero-based index and injected
instant as in-flight. If that write fails, the delegate is not called. A returned current, withdrawn,
stopped, or canceled cycle is then committed as one ordered observation.

### C4 — completed work resumes only from its next clean boundary

Current and withdrawn outcomes are committed and never replayed. If budget remains, the journal moves
to waiting; the injected delay prepares and persists the next instant, after which the next cycle may
start. Reopening ready or waiting state preserves the completed observations and continues at the
same next index. A canceled wait changes no durable state.

### C5 — an interrupted effect is indeterminate and inert

Opening an in-flight journal reports `durable-cadence-indeterminate`. No delay or cycle is invoked and
no cursor is advanced merely by opening it. The stored attempted index and instant remain observable;
the absence of a committed observation is not interpreted as absence of external effects.

### C6 — retry or abandonment requires explicit reconciliation

Only an in-flight journal accepts a reconciliation decision. `retry` records one interruption and one
retry, then makes the same index and instant ready; it does not invoke the cycle itself. `abandon`
records one interruption and terminates the run as `durable-cadence-abandoned`. A gap completed before
an abandoned attempt remains observable even though no cycle observation follows it.

### C7 — terminal recovery is idempotent and effect-free

Reaching the cycle budget commits `durable-cadence-complete`. A stopped or canceled cycle commits
`durable-cadence-stopped` or `durable-cadence-canceled`. Reopening any terminal journal returns the
same snapshot, and later advance or reconciliation calls report the terminal state without invoking
a delay or cycle or changing bytes.

### C8 — both roots execute one shared recovery model

Reference C# and Minimal F# independently consume the shared transition vectors and report the
terminal code, phase, ordered cycle codes and instants, completed gaps, next cycle index, interruption
count, and retry count.

## Phase-wide properties

- No journal contains more observations than its declared cycle budget.
- Every committed observation has a unique index, and reopening never changes their order.
- A cycle delegate is called only while the durable image already names that index as in-flight.
- No completed observation is replayed by recovery.
- Every retry repeats the interrupted index and instant; it never skips or invents a cycle.
- Every refusal and canceled wait preserves the exact prior durable bytes.
- A terminal or indeterminate open has zero delay and cycle effects.

## Deliberate limits

CBI48 provides the durable fact needed by later policy; it does not decide whether an indeterminate
cycle is safe to retry, how long unavailable policy may leave providers serving, or whether a
withdrawn provider should restart under a successor. Those remain explicit offline/reconciliation
and restart-policy work. Cross-process ownership, endpoint and key rotation, privileged recovery-
floor custody, and production isolation remain separate security boundaries.
