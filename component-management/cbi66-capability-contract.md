# CBI66 capability contract — retry-aware cadence gaps

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI49 publishes a next retry instant with every continuation it permits, capped at the deadline by its
own C1 property. Nothing has ever consumed it. CBI64 recorded that as a limit and named the reason:
CBI48's journal validated that every recorded gap equalled the schedule interval, so honouring the
retry instant was a change to a durable invariant rather than a cycle's work.

The cost is not cosmetic. A host with a grace of five minutes and an interval of an hour is asking for
service to stop five minutes after the endpoint goes away, and gets fifty-five extra minutes of
service instead, because the cadence's next look lands long after the deadline it was meant to
enforce. CBI66 makes the cadence look when the policy says to look.

Reading CBI48 to do it found a defect underneath. `CompleteGap` validates the instant it is given and
then records the schedule interval regardless, so a gap that is not the interval is recorded as one
that is. That is inert while every gap equals the interval and wrong the moment one does not, and it
is fixed first.

This is not a clock, a timer, a daemon, or a claim that a host waits accurately.

## Capabilities

### C1 — the journal records the gap that elapsed

`CompleteGap` records the difference between the instant it is given and the one it held, rather than
the schedule interval. Validation accepts a positive gap no greater than the interval instead of
requiring equality.

The defect this fixes was pinned by a failing test before it was fixed: a twenty-second gap on a
sixty-second schedule was recorded as sixty, and the recorded gaps then disagreed with the recorded
cycle instants in the same journal.

Property: every recorded gap equals the difference between the prepared instants either side of it.

### C2 — the cadence honours the retry instant CBI49 publishes

When the cycle just run carries an availability observation naming a retry instant, the next gap is
the earlier of the schedule interval and that instant. CBI49 caps the retry instant at the deadline,
so the cadence lands *on* the deadline rather than at the first scheduled cycle after it.

Property: within one run, no cycle is scheduled later than the deadline reported by the cycle before
it, unless a cycle has already reported expiry.

### C3 — the gap is only ever shortened

The schedule interval is the host's upper bound and remains one. A retry instant further away than
the interval changes nothing, because a policy that only ever asks to be consulted *sooner* must not
be able to slow a host's own schedule down.

Property: every gap is positive and no greater than the schedule interval.

### C4 — shortening is a property of the outage, not a mode

A cycle that established current policy carries no availability observation, so nothing shortens the
gap after it and the ordinary interval resumes. The cadence does not enter or leave a state.

Property: a run in which no cycle reports availability produces exactly the gaps CBI47 pinned.

### C5 — a journal written before this slice stays valid

Its gaps all equal its interval, which is inside the bound C1 accepts, and no format marker moves. A
host upgrading reads its own record unchanged.

Property: the durable encoding of a run whose gaps are all the interval is byte-identical to the one
the previous implementation wrote.

### C6 — expiry is observed at the deadline

Composed, a cadence whose interval is longer than its grace still stops at the deadline. This is the
capability the slice exists for and the one a host configures grace expecting.

Property: for any interval, grace, and retry CBI49 accepts, the instant at which a run first reports
`offline-grace-expired` is the deadline itself.

### C7 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the cadence code, the
ordered cycle instants, the gaps, and the instant at which expiry was observed.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

A gap is a duration the cadence asks its host to wait, and the host is what waits: nothing here is a
timer, and a host that waits inaccurately is recorded by the instants it reports rather than corrected.
CBI49's retry interval is the only thing that shortens a gap; no poll or rotation backoff does, because
those are bounded inside one cycle and CBI41 and CBI60 already own them.

A non-positive retry gap is unreachable, because CBI49 issues a retry instant only while the evaluating
instant is strictly inside grace and caps it at the deadline. No branch is added for it; the cadence's
existing requirement that a delay advance the cycle instant is the nearest observable guard and is
where such a decision would surface.

Cross-process ownership of the serving set, privileged custody, and CBI64's other limits are unchanged.
