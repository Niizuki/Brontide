# CBI61 contract-completeness review

Date: 2026-08-06

Status: complete

This review asks what the CBI61 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **The order of the two loops is decidable from the registry, and had to be, because a preference
  would have been invisible.** A policy update is verified against the authority in force. Polling
  first refuses an update signed by the authority a pending rotation installs, and the refusal is
  indistinguishable from a stranger's. Rotating first applies it. The contract states the order and
  the shared vector distinguishes the two orders by whether a sequence was applied, so the claim is
  falsifiable rather than declared.
- **CBI41's `foreign-authority` vector became half-right when CBI57 landed.** Its name records what
  `policy-update-authority-mismatch` could mean before an authority could rotate. Afterwards the same
  observable also describes a legitimate publisher a host has not caught up with. CBI41 is not wrong
  and is not changed — failing closed on an update it cannot verify is correct either way — but its
  vector's *name* is now a description of one of two causes, and nothing below the composition can
  tell them apart. This is the first case in the programme where a later slice made an existing
  refusal ambiguous without making it incorrect.
- **Attribution must be a conjunction of recorded facts, not an inference about the update.** The
  cycle reports `authority-behind` exactly when the poll refused with that code *and* the same
  cycle's rotation did not reach current. A rule that looked only at the poll code would relabel a
  stranger's update as a rotation lag, which is the more dangerous direction, and the vector pair
  that differs only in the rotation outcome is what forces the distinction. Without the
  `foreign-authority-is-not-attributed` vector both readings pass.
- **Which rotation outcomes are fatal follows from what each changed.** A refused or exhausted
  rotation changed nothing — no authority, no chain, no floor, no member — so the cycle proceeds and
  the failure is recorded rather than propagated. An unretained floor changed the chain without the
  guard that describes it, which is CBI41's own reason for stopping on its own floor handoff. The
  contract states both with their reasons rather than listing codes.
- **A cycle may succeed while carrying a failure.** `provider-trust-cycle-current` with a refused
  rotation beside it is the ordinary case for a host whose rotation endpoint is down and whose policy
  endpoint is not. Saying so explicitly is what keeps a later implementer from promoting the rotation
  code into the cycle code for tidiness.
- **The cadence itself needed no change.** CBI47's loop is generic over a cycle, so governing it is a
  new cycle rather than a new cadence, and the budget, gap accounting, stop, and cancellation rules
  are CBI47's unchanged. The one change to CBI47 is that a cycle result now carries the rotation it
  ran and admits an absent poll, which is what makes the pairing structural rather than positional.

## What the phase deliberately does not decide

Whether a host should keep serving while its rotation endpoint is unreachable is CBI49's offline
question and is not answered here: this slice retires nothing. Whether a cadence resumes after a
restart is CBI48's. Two hosts polling one registry remain outside the claim, as CBI38's one-writer
bound already is.

## Residual limits

The attribution is exact for the case it names and silent about a host that is behind *and* being
sent a stranger's update in the same cycle; the poll refuses either way and the cycle reports
`authority-behind`, which names the cause the host can act on. Privileged custody of either floor
remains the named deployment boundary. The next bounded implementation boundary is durable
resumption of a governed cadence, where CBI48's journal must record which of the two loops a
resumed cycle had already run.
