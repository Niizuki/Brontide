# CBI64 capability contract — cadence availability enforcement

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI63 named the next boundary as "a host that terminates providers when CBI49's grace expires". CBI50
already does that, and has since 2026-08-05: its C3 stops every admitted member on
`offline-grace-expired`. The sentence was copied forward from CBI49's own deliberate-limits section,
which was never revised when the slice that discharged it landed. It is the fifth stated limit in this
programme that described how something was called rather than a rule anything applied, and the second
one the programme wrote about itself.

What no slice has taken is the composition. **Nothing that polls repeatedly has ever reached CBI49.**
CBI47's cycle maps every non-current poll to `provider-trust-cycle-stopped` and the cadence ends
there, so the grace interval, the deadline, and CBI50's enforcement are unreachable from the only
component in the programme that evaluates availability more than once. A transport outage therefore
ends the loop and leaves every provider serving with no deadline — which is neither of the two answers
CBI49 offers. CBI64 supplies that composition.

This is not a clock, a network monitor, a daemon, a restart controller, a trust decision, a
cross-process owner, or a durable record of an availability stop.

## Capabilities

### C1 — the cadence supplies the last-current instant from its own history

CBI49 requires the last instant at which a cadence cycle established current policy, and nothing has
ever supplied one. The cycle records the instant of each cycle whose poll reported current, and an
outage is evaluated against it. An outage before any cycle was current has no baseline, so CBI49
reports `offline-service-stop-required` and service stops — the fail-closed direction that contract
already chose.

Property: every offline decision in a run was evaluated either against the instant of an earlier
cycle in that run whose poll was current, or against no baseline at all.

### C2 — a running cadence cannot extend its own deadline

An outage does not refresh the baseline. Every cycle of one outage evaluates against the same instant,
so the deadline is constant across the outage and expiry arrives on schedule. A cadence that took each
cycle's own instant as the baseline would report `offline-existing-service` forever, and no vector
that evaluates once can tell the two apart.

Property: across one run, the deadline reported by consecutive offline decisions never moves while no
cycle establishes current policy.

### C3 — availability is decided only where there is a poll outcome to decide it

A canceled cycle stays a cancellation and enforces nothing: cancellation is the host stopping its own
loop, not the endpoint failing. A governed cycle whose rotation stopped it before the policy endpoint
carries no poll, and CBI49 has no observation for a cycle that made none.

Property: every cycle carrying an availability observation carries a poll, and no canceled cycle
carries one.

### C4 — every other non-current poll reaches one decision and one enforcement

A grace-eligible outage inside grace continues, over a non-empty serving set (`offline-existing-
service`) or an empty one (`offline-idle`). Expiry stops every member. Every ineligible outcome —
authentication failure, registry refusal, a stale reply, an unretained policy floor — is
`offline-service-stop-required` and stops every member too. Routing only the eligible third would
leave CBI49's other two answers unreachable from a cadence, which is the composition deciding
availability where nothing can see it.

Property: every non-current, non-canceled cycle that carries a poll carries exactly one availability
observation whose enforcement code is one CBI50 produces.

### C5 — the code names why policy could not be established; availability is a separate observation

`provider-trust-cycle-offline` is the one new code and it names the one new outcome: a cadence that
continues through an outage. Every other cycle keeps the code its poll produced, so CBI61's
`provider-trust-cycle-authority-behind` attribution survives unchanged even in the same cycle that
stopped every member. The pairing is structural rather than positional, as CBI61 made the rotation's,
and the availability wrapper is outermost precisely because the code it must not disturb is the one
the governed wrapper computes.

Property: no availability outcome changes a cycle code except to `provider-trust-cycle-offline`, and
that code is reachable only from a decision that permits continuation.

### C6 — continuation is existing service only

Inside grace nothing is retired, terminated, or removed, and no path acquires, launches, admits, or
restarts. Staged artifacts survive expiry, because an availability stop is not a publisher
withdrawal — CBI50's own rule, which is what leaves CBI51 a way back.

Property: every `provider-trust-cycle-offline` cycle stopped no member and left every admitted member
serving, and no cadence path reports a started provider.

### C7 — the new code is in the one vocabulary

`provider-trust-cycle-offline` is added to the vocabulary CBI62 established, so CBI48's journal
accepts a cadence that continued through an outage. Producing it from a literal instead is exactly
what CBI61 did and what CBI62 had to repair; the guard walks the vocabulary, so it needed no edit.

Property: every code a cadence cycle can return is known to the journal, and the offline code
continues the cadence.

### C8 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the cadence code, the
ordered cycle codes, each cycle's decision and enforcement codes, the stopped counts, and whether the
deadline moved.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

Availability is evaluated at the cadence's own interval. CBI49's retry instant is reported and is not
used to shorten the gap: CBI48's journal validates that every recorded gap equals the schedule
interval, so honouring it is a change to the durable gap invariant and its vectors rather than a
cycle's work. Expiry is therefore observed at the first cycle at or after the deadline, within one
interval of it; a host wanting tighter enforcement sets an interval no longer than its retry.

The baseline is run-local. A durable cadence resumed after a crash begins with none, so its first
outage stops service rather than entering grace. Deriving a baseline from CBI48's committed
observations is a later boundary, and the present behaviour is CBI49's own answer to a missing one.

A cycle that never reached the policy endpoint enforces nothing, so an unretained authority floor
stops the cadence with every provider still serving. Inventing an observation for it would decide
availability from a rotation outcome.

The cycle reports the availability decision and its counts rather than CBI50's per-member
observations, which keeps CBI47's cycle result independent of the enforcement component's types.
Per-member evidence remains CBI50's, where its ordering and failure-isolation properties are pinned.

Nothing here restarts a stopped provider, records the stop durably, or owns the serving set across
processes.
