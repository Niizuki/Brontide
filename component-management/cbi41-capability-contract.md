# CBI41 capability contract — host-owned policy poll scheduler

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI41 gives CBI39 one host-owned poll cycle: a bounded sequence of distribution attempts that
advances the durable CBI38 registry until the endpoint reports the host current, retries only the
failures a fresh attempt can change, backs off deterministically between them, and hands each newly
published recovery floor to a host retention sink.

This is not a background service, thread, timer, hosted worker, or supervision tree. It is not an
availability guarantee, an endpoint discovery or rotation scheme, a trusted clock, a jitter or
load-shedding policy, secure floor custody, cross-process coordination, or a replacement for CBI39
authentication, CBI37 authority, or CBI38 publication. One call performs one cycle and returns; when
the next cycle runs is the host's decision.

## Capabilities

### C1 — one bounded cycle advances until the endpoint reports the host current

Each attempt is one CBI39 `SynchronizeAsync` under the schedule's attempt timeout. An applied update
is progress, so the cycle immediately re-reads the advanced cursor and attempts again; the cycle ends
when the endpoint reports the host current, when a terminal outcome occurs, when the attempt budget
is spent, or on cancellation. The result names the outcome, the attempts spent, every gap waited,
every sequence applied, every sequence retained, and the last attempt's own CBI39 code.

### C2 — retry is bounded, deterministic, and reserved for failures a fresh attempt can change

A retry changes exactly three things: a new random challenge, a freshly read cursor, and whatever the
network does next. Transport failure, attempt timeout, a stale validity window, and a superseded
cursor are therefore retried, and nothing else is. The gap before retry *n* is
`min(baseDelay × multiplier^(n-1), maximumDelay)` computed from the consecutive-failure count alone,
with no jitter, so both realizations produce the identical gap sequence. **Progress resets the
count**, because backoff exists to back away from a peer that is not answering and a peer that just
answered is answering.

### C3 — a terminal outcome ends the cycle at the attempt that produced it

Every CBI39 endpoint-authentication outcome and every CBI37/CBI38 registry refusal ends the cycle
immediately, consuming no further budget and waiting no further gap. Repeating a request the pinned
endpoint key already failed to authenticate cannot change the answer and would only send more traffic
to an unauthenticated peer; repeating an update the registry refused on its own state would be
refused identically.

### C4 — the success floor is handed off after publication and never before

Each applied update publishes its CBI38 checkpoint first; only then is the resulting floor offered
once to the host sink. **A floor is a statement about what the host durably holds, so it cannot
precede the thing it describes.** A floor retained ahead of publication and interrupted by a crash
claims a state no checkpoint records, and CBI38 recovery reads that as
`policy-checkpoint-rollback-detected` — a refusal to open a checkpoint nothing rolled back. Handoffs
within a cycle are strictly increasing in sequence.

### C5 — a refused handoff stops the cycle and reports advanced-but-unretained

A sink that fails or is canceled leaves an update already published and live. The cycle does not undo
it, because it is durable, and does not continue, because every later advance would move further past
a floor the host does not hold. The result reports the applied sequence with no matching retained
sequence, which is the exact diagnostic a host needs to re-establish its floor.

### C6 — cancellation is observed before every attempt and inside every gap, and reaches no provider

Cancellation requested before the first attempt produces a canceled cycle with zero attempts and no
call to the source. Cancellation during a gap ends the cycle without recording that gap. The cycle
holds no ambient clock: its instant starts at the caller's and advances only through the injected
delay, so the whole cycle is a function of what the host injects.

### C7 — both roots produce identical cycle observations for the shared vectors

Reference C# and Minimal F# independently consume the shared schedule and the fourteen shared
vectors, and independently compute the outcome code, attempt count, gap sequence, applied sequences,
retained sequences, final registry sequence, and last attempt code for each.

## Phase-wide properties

- No vector spends more attempts than the schedule's budget.
- Every vector records exactly `max(attempts - 1, 0)` gaps, so a gap without an attempt after it, or
  an attempt without a gap before it, is a defect rather than an unstated case.
- Every recorded gap equals the schedule's value for the consecutive-failure count in force at that
  point and never exceeds the maximum delay.
- Retained sequences are a prefix of applied sequences, and both are strictly increasing.
- For every vector whose source performs no write of its own, the registry's final sequence equals the
  last applied sequence, or zero when the cycle applied nothing: the cycle advances nothing it does
  not report.
- A refused or unretained outcome is always the cycle's last attempt.
