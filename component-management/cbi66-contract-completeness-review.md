# CBI66 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI66 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **A fact the model published and nothing consumed turned out to hide a defect underneath it.** CBI49
  has issued a retry instant with every continuation since it was written, and CBI64 recorded that
  nothing used it. Reading CBI48 to change that found `CompleteGap` validating the instant it is given
  and then recording the schedule interval regardless — inert while every gap equalled the interval,
  and wrong the moment one did not. It was pinned with a failing test before it was fixed: a
  twenty-second gap on a sixty-second schedule was recorded as sixty, and the failure named that
  mechanism rather than a symptom.
- **The bound is one-sided, and saying which side is the capability.** A retry instant may bring a
  cadence's next look forward and may never push it back: the interval is the host's own schedule and a
  policy that only ever asks to be consulted *sooner* must not be able to slow it down. The vector that
  fails when this is wrong is the one where the retry is longer than the interval, and without it a
  gap-lengthening implementation passes everything else.
- **A cadence cannot detect an outage before it looks, and the contract says so rather than claiming
  the deadline is always met.** The first outage cycle still falls on the ordinary interval, because
  the cycle before it established current policy and carried nothing to shorten the gap. Where the
  interval exceeds grace the deadline can therefore pass before any outage is seen at all, and a
  vector states that outcome instead of leaving C6 to be read as a guarantee it is not.
- **Shortening is a property of being inside an outage rather than a mode.** A cycle that establishes
  current policy carries no availability observation, so the ordinary interval resumes with no state to
  leave. The vector that shows it recovers mid-run and returns to the interval, which also serves as
  the regression guard for every gap CBI47 pinned.
- **The durable change is a relaxation, so the migration is that there is none.** A journal written
  before this slice has gaps all equal to its interval, which is inside the bound validation now
  accepts, and no format marker moves. C5 pins the direction: a guard requiring gaps strictly *below*
  the interval would invalidate every record already on disk, and that mistake passes every other test.
- **A non-positive retry gap is unreachable and is not given a branch.** CBI49 issues a retry instant
  only while the evaluating instant is strictly inside grace and caps it at the deadline. Manufacturing
  a refusal for it is the defect PB6 found three of; the cadence's existing requirement that a delay
  advance the cycle instant is the nearest observable guard and is named as the place such a decision
  would surface.

## What the phase deliberately does not decide

How long a host actually waits. A gap is a duration the cadence asks for, and a host that waits
inaccurately is recorded by the instants it reports rather than corrected. Nothing here is a timer.

## Residual limits

Only CBI49's retry interval shortens a gap. CBI41's poll backoff and CBI60's rotation backoff are
bounded inside one cycle and remain owned by those slices; nothing was widened to let them reach the
cadence loop.

The shared vectors run over an empty serving set, so CBI49 reports `offline-idle` and no provider
process is launched. The gaps they pin are therefore only the gaps a serving cadence waits if CBI49's
retry instant does not depend on how many members are serving — a claim about a dependency rather than
about this slice, so a named test in each root evaluates the same outage at both serving counts and
requires the deadline and retry instant to be equal while the decision codes differ. Testing the claim
directly is what makes the vectors' silence about members safe; re-running a whole cadence over real
providers would observe the same equality less directly and prove no more.

Cross-process ownership of the serving set and privileged custody remain separate.
