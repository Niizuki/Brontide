# CBI64 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI64 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C8.

## Findings closed in the contract

- **The item this slice was given was already done, and the real gap was next to it.** CBI63 named "a
  host that terminates providers when CBI49's grace expires" as the next boundary. CBI50 has done
  exactly that since 2026-08-05, and the sentence came from CBI49's deliberate-limits section, which
  was never revised when CBI50 landed. Reading CM4's models rather than the forward reference shows
  the actual hole one step over: **CBI49 and CBI50 exist and nothing that polls repeatedly has ever
  called them.** The check that would have caught it is cheap and is now the practice this review
  records — when a slice names its successor, the successor reads the *named* slice's own contract
  rather than the pointer, because a pointer is written before the work and never revisited
  afterwards.
- **A cadence that stops is not a cadence that decided.** Before this slice a transport outage ended
  the loop with `provider-trust-cycle-stopped` and every provider still serving. That is neither of
  CBI49's answers: not continuation, because nothing said service could continue, and not a stop,
  because nothing stopped. The contract states that the outage now produces one of the two, so a host
  cannot be left in a third state the model does not name.
- **Only a repeated evaluator can exercise CBI49's own C3 property.** "Repeated evaluation uses the
  original last-current instant" is a claim about a caller that evaluates more than once, and CBI49's
  vectors evaluate once each, so nothing had ever tested it. A cadence is exactly the caller that can
  get it wrong, and getting it wrong is invisible in every single-cycle vector: the deadline simply
  never arrives. C2 pins the property where it can now fail, and a deliberate defect that refreshed
  the baseline was watched turning the expiring vector into an endless one.
- **Routing only the grace-eligible outcomes would have decided availability where nothing can see
  it.** It is the tempting reading — grace is what this slice is about — and it leaves CBI49's other
  two answers unreachable from any cadence. The contract routes every non-current poll that made a
  poll, so `offline-service-stop-required` is produced by the composition rather than only by a
  direct caller.
- **Two facts, two places.** A cycle code answers why current policy could not be established and an
  availability observation answers what was done about the providers. Collapsing them would have cost
  CBI61's `provider-trust-cycle-authority-behind` attribution, because that refusal is never
  grace-eligible and so always coincides with a stop. The wrapper is therefore outermost, which is
  stated as the reason rather than left as an ordering a later refactor could reverse.
- **Cancellation is the host, not the endpoint.** CBI49 would classify a canceled poll as
  `offline-service-stop-required` and CBI50 would then terminate every provider, so an ordinary
  shutdown request would become an availability withdrawal. The contract puts cancellation ahead of
  the evaluation and a vector pins that every provider is left serving.
- **A cycle that reached no endpoint has nothing to evaluate.** CBI61's rotation can stop a cycle
  before the policy endpoint is contacted, and CBI49 has no observation for a poll that was never
  made. Deciding availability from a rotation outcome would be inventing one, so the contract states
  the gap instead — and it is a fail-open, named as such below rather than implied away.
- **A refused serving snapshot decides nothing.** CBI50 refuses a duplicate or non-serving snapshot
  before evaluating any policy, so the composition can produce a cycle with an enforcement
  observation and no decision at all. The contract says the decision is absent rather than letting a
  reader assume every enforcement carries one, and a vector pins that nothing is stopped.

## What the phase deliberately does not decide

Whether to evaluate sooner than the cadence interval remains the host's, expressed as its choice of
interval. Whether a stopped provider comes back is CBI51's and CBI52's, which is why CBI50's
artifact retention matters and why this slice removes nothing.

## Residual limits

CBI49's retry instant is reported and unused, because CBI48's journal validates that every recorded
gap equals the schedule interval; honouring it is a change to that durable invariant and its vectors
rather than a cycle's work. Expiry is observed at the first cycle at or after the deadline.

The baseline is run-local, so a durable cadence resumed after a crash has none and its first outage
stops service. That is CBI49's own answer to a missing baseline and the safe direction, but it means
composing CBI64 with CBI62 makes a crash cost a serving set at the next outage; deriving a baseline
from CBI48's committed observations is the next bounded boundary here.

A cycle its rotation stopped enforces nothing, so an unretained authority floor stops the cadence
with every provider still serving. Cross-process ownership of the serving set, a durable record of an
availability stop, and privileged custody of either floor remain separate.
