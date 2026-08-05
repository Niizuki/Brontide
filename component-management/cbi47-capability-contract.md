# CBI47 capability contract — provider trust cadence

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI41 performs one bounded publisher-policy poll cycle. CBI46 performs one bounded serving-set trust
sweep. CBI47 gives a host one bounded cadence that composes those operations repeatedly: the first
cycle runs immediately, each later cycle waits one injected interval, a current policy is established
before the current serving set is swept, and every cycle remains observable.

This is an in-process host loop with an explicit 1-64 cycle budget. It is not an operating-system
service, durable job queue, crash-resumption protocol, availability or offline policy, restart
controller, endpoint or authority-key rotation scheme, privileged floor custodian, or production
process sandbox.

## Capabilities

### C1 — cadence is bounded and explicit

A schedule accepts 1-64 cycles and a positive interval no greater than 24 hours. The result records
every completed cycle and every completed inter-cycle gap. Reaching the requested cycle count reports
`provider-trust-cadence-complete`; there is no hidden next invocation.

### C2 — the first cycle is immediate and later cycles use injected time

The first cycle receives the caller's start instant without waiting. Before every later cycle, the
host-provided delay waits exactly the configured interval and returns the next instant. No ambient
clock, timer, or jitter is read by the semantic loop.

### C3 — current policy precedes any serving sweep

Each cycle runs CBI41 first. Only `policy-poll-current` permits the serving-set operation. A refused,
exhausted, canceled, or unretained poll produces no serving-set snapshot or sweep and stops the
cadence, preserving the complete CBI41 result.

### C4 — the current serving set is swept once

After a current poll, the host snapshots the serving set and invokes CBI46 once. An empty snapshot is
a successful no-op and does not call CBI46, whose own non-empty precondition remains unchanged. A
non-empty snapshot preserves the CBI46 result without replacing any member observation.

### C5 — successful withdrawal does not stop cadence

Both `serving-trust-sweep-current` and `serving-trust-sweep-withdrawn` complete a cycle and permit the
next scheduled cycle. Withdrawal is successful fail-closed enforcement, not an orchestration fault.

### C6 — an invalid or incomplete sweep stops before another gap

`serving-trust-sweep-invalid`, `serving-trust-sweep-incomplete`, or
`serving-trust-sweep-cleanup-incomplete` produces `provider-trust-cycle-stopped`. The cadence records
that cycle and returns `provider-trust-cadence-stopped` without waiting another interval.

### C7 — cancellation has an exact boundary

Cancellation before the first cycle returns canceled with no cycle or gap. Cancellation during a
gap records neither the interrupted gap nor another cycle. A canceled CBI41 result is recorded as the
last cycle and then cancels the cadence. Cancellation does not roll back an earlier policy update,
floor retention, withdrawal, or cleanup observation.

### C8 — both roots agree

Reference C# and Minimal F# independently consume the shared schedule vectors and report the cadence
code, ordered cycle codes, completed gap durations, cycle instants, and whether cancellation or a
stopped cycle ended the run.

## Phase-wide properties

- No result contains more cycles than the schedule budget.
- Every result records exactly `max(cycles - 1, 0)` completed gaps unless cancellation interrupts the
  next gap; no completed gap exists without the cycle that follows it.
- Cycle instants are strictly ordered by the delay implementation's returned instants.
- No serving-set source or sweep is reached unless that cycle's policy poll reports current.
- Every completed non-empty serving-set operation retains exactly one CBI46 result.
- A stopped or canceled cycle is always the final recorded cycle.

## Deliberate limits

CBI47 does not decide when another bounded run starts after process restart, how an unavailable
policy affects already-serving members, whether cleanup is retried, or whether withdrawn providers
restart under a successor publisher. Those are durable retry/resumption, offline policy, and restart
decisions. Endpoint/key rotation, privileged recovery-floor custody, and production isolation remain
separate security work.
