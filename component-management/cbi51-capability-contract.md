# CBI51 capability contract — provider restart policy

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI50 stops service when offline availability no longer permits it and deliberately retains staged
artifacts. CBI51 decides whether a stopped serving activation may be considered for restart. A
restart is a new effect: retained policy alone is insufficient, so the caller must prove that one
successful current-policy cycle observed the exact policy the registry now exposes. The retained,
verified publisher evidence is then evaluated again against that policy.

This policy is effect-free. It does not launch a process, reactivate a lifecycle, acquire content,
or treat retained bytes as authority.

## Capabilities

### C1 — restart policy is explicit and bounded

A policy accepts one through eight attempts and a positive delay no greater than one hour.
Evaluation receives injected time, a stopped opaque serving activation, a typed restart cause, the
exact policy identity established by the current cycle, and the prior attempt observation.

Property: no result authorizes more than the configured number of attempts.

### C2 — a current-cycle proof is required

The supplied policy identity must equal the matching registry's current policy identity. Missing,
stale, foreign-authority, or mismatched proof refuses restart before publisher evaluation.

Property: retained policy without an exact current-cycle proof never produces restart readiness.

### C3 — only recoverable stop causes are eligible

`UnexpectedExit` and `OfflineAvailability` are eligible. `PublisherTrustWithdrawal` and
`OperatorRetirement` are terminal for this policy and return `provider-restart-cause-refused`.
A still-serving activation returns `provider-restart-not-required`.

Property: no terminal cause produces restart readiness.

### C4 — publisher trust is decided again

The activation's internally retained verified evidence is evaluated against the proven current
policy. Revoked, omitted, mismatched, or unavailable evidence refuses with CBI35 attribution. The
authorization must name the retained staged identity.

Property: every ready result carries a current authorization for the exact retained content.

### C5 — attempt delay and exhaustion are deterministic

Attempt zero may be ready immediately. After a failed attempt, the next attempt is waiting until
`last-attempt + delay`; at or after that instant it is ready. Reaching the configured attempt count
is `provider-restart-exhausted`.

Property: changing only time can move waiting to ready, never ready back to waiting.

### C6 — the policy has zero provider and artifact effects

Evaluation does not launch, retire, terminate, acquire, stage, remove, or mutate the registry. It
returns only an inspectable decision and optional retry instant.

Property: every vector preserves provider state, staged content, and registry identity.

### C7 — invalid observations fail closed

Negative or over-budget attempt counts, a missing last-attempt instant after a reported attempt, an
instant supplied for attempt zero, a future last-attempt instant, or an unrepresentable retry instant
is `provider-restart-observation-invalid` with no authorization.

Property: every invalid observation denies restart and has no retry instant.

### C8 — both roots execute one shared restart model

Reference C# and Minimal F# independently execute shared vectors covering readiness, waiting,
exhaustion, cause refusal, trust refusal, current-proof mismatch, still-serving state, and invalid
observations.

Property: every shared vector produces the same code, origin, readiness, and retry observation.

## Contract-completeness review

The contract distinguishes retained policy from proven current policy, stopped from still-serving,
availability from trust withdrawal, content retention from publisher authority, and eligibility
from the relaunch effect. It states the first attempt, delay boundary, exhaustion, malformed history,
future time, and overflow cases. It deliberately does not choose a provider executable, rebuild a
lifecycle request, persist attempt history, coordinate concurrent owners, or define crash recovery
during launch. Those belong to restart enforcement and durable supervision, not silent CBI51 work.

## Deliberate limits

CBI51 is one caller-triggered decision. It is not a scheduler, launch controller, process supervisor,
durable retry journal, cross-process lease, endpoint/key rotation scheme, privileged floor anchor, or
production sandbox.
