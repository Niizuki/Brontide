# CBI49 capability contract — provider-trust offline and reconciliation policy

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI48 exposes a durable cadence boundary but deliberately cannot decide whether an unavailable
publisher-policy endpoint permits existing providers to remain serving, or whether an interrupted
cycle is safe to retry. CBI49 supplies that host policy. It gives availability failures one explicit,
bounded grace interval derived from the last successfully current cycle, and it translates matching
host reconciliation evidence into CBI48's retry or abandonment transition.

The retained publisher policy has no intrinsic expiry, so CBI49 does not call it fresh. Offline grace
is a host availability choice, never trust evidence. It permits only already-serving providers to
remain in place; it authorizes no acquisition, launch, admission, restart, or skipped trust sweep.
Reconciliation evidence is an injected host observation, not proof manufactured by the journal.

This is not a clock, network monitor, daemon, provider restart controller, exactly-once protocol,
secure evidence custodian, cross-process owner, endpoint/key rotation scheme, privileged floor
anchor, or production sandbox.

## Capabilities

### C1 — offline policy is explicit and bounded

An offline policy accepts a positive retry interval and grace interval, each no greater than 24
hours, with retry no longer than grace. Evaluation receives an injected instant, the last instant at
which a cadence cycle established current policy, the poll outcome, and a 0-64 current serving count.
A future last-current instant, an out-of-range count, or an instant from which the deadline cannot be
represented is `offline-observation-invalid` rather than an escaping time-arithmetic failure.

Property: every non-idle continuation has a deadline no later than last-current plus grace and a
next retry instant no later than that deadline.

### C2 — only endpoint unavailability is grace-eligible

Grace is considered only for `policy-poll-exhausted` whose last attempt is
`policy-distribution-transport-failed` or `policy-distribution-timeout`. Cancellation, stale replies,
superseded cursors, authentication failure, registry refusal, and an unretained floor stop visibly;
none is relabelled as offline availability.

Property: no terminal trust or integrity outcome produces `offline-existing-service`.

### C3 — grace requires a prior current observation and never refreshes it

Without a last-current instant, service stops. Before `last-current + grace`, an eligible outage with
existing service reports `offline-existing-service`; at or after the deadline it reports
`offline-grace-expired`. Repeated evaluation uses the original last-current instant and cannot extend
the deadline.

Property: changing only the evaluation instant can move a decision from serving to expired, never
from expired back to serving.

### C4 — offline continuation is existing-service-only

Within grace, a positive serving count may remain serving until the earlier of the retry interval or
deadline. An empty serving set reports `offline-idle` and authorizes no later launch. Expiry reports
`offline-grace-expired`; an ineligible outcome reports `offline-service-stop-required`. The result
carries explicit
`MayContinueExistingService` and `MayStartProvider` facts; the latter is always false.

Property: no CBI49 offline vector authorizes a provider start or changes durable trust state.

### C5 — reconciliation evidence names the interrupted attempt exactly

Evidence carries the distinct run identity, attempted cycle index, attempted instant, and one of
`no-effects-confirmed`, `effects-accounted-for`, or `unknown`. It is accepted only against an
in-flight snapshot with the same three facts. Evidence for another run, index, instant, clean phase,
or terminal phase changes no journal state.

Property: mismatched or unnecessary evidence preserves the exact prior durable bytes.

### C6 — unknown evidence leaves the interruption inert

`unknown` reports `cadence-reconciliation-deferred`. It invokes neither retry nor abandonment and
leaves the in-flight marker unchanged so a later, stronger observation can be supplied.

Property: deferred reconciliation has zero cadence-cycle and delay effects.

### C7 — conclusive evidence selects one CBI48 transition

`no-effects-confirmed` selects CBI48 retry; `effects-accounted-for` selects abandonment. The policy
does not invoke the cycle. A successful decision increments CBI48's interruption count exactly once,
and only retry increments its retry count. Reapplying evidence after the transition is effect-free
because the journal is no longer in-flight.

Property: one accepted reconciliation produces exactly one durable CBI48 transition and no provider
effect.

### C8 — both roots execute one shared policy model

Reference C# and Minimal F# independently consume the shared CBI49 vectors and report the offline
code, continuation/start facts, deadline and retry instant, reconciliation code, journal phase, and
interruption/retry counts.

Property: every declared vector produces the same portable observation in both roots.

## Deliberate limits

CBI49 makes the availability decision inspectable but does not itself terminate providers when grace
expires; that effect belongs to host supervision and is the next integration boundary. It does not
restart a withdrawn provider, manufacture reconciliation evidence, or prove external effects absent.
Provider restart policy remains next work. Cross-process ownership, endpoint and authority-key
rotation, privileged floor custody, and production isolation remain separate security boundaries.
