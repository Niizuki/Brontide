# CBI60 contract-completeness review

Date: 2026-08-06

Status: complete

This review asks what the CBI60 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **CBI41's retryable set does not transfer by analogy, and had to be re-derived.** The two cycles
  agree on transport failure, timeout, staleness, and a superseded cursor, but a rotation cycle also
  faces the native CBI57 refusals a policy cycle never sees. They are not retryable for CBI41's own
  reason rather than by inheritance: the endpoint would offer the same statement again. Stating it
  per outcome rather than by reference to CBI41 is what keeps a later category from being retried
  because the two lists looked alike.
- **A cycle over a second key needs its own vocabulary, not CBI41's codes.** `policy-poll-*` describes
  a policy cursor; a rotation cycle reports generations. Separate `policy-authority-cycle-*` codes
  keep a host from reading one cycle's exhaustion as the other's.
- **A guard introduced after the thing it guards cannot use absence as evidence.** CBI42 orders its
  establishment before the first checkpoint, which is exactly what makes a later absence mean the
  guard was removed. That ordering is unavailable here, so absence beneath an existing checkpoint is
  adopted at zero. Reporting it as `policy-authority-floor-adopted` rather than as a recovery is the
  whole of the difference the contract can honestly claim: the host is told the guard did not exist.
- **What adoption costs is bounded by the chain rather than asserted.** The review asked what an
  attacker gains by deleting only this guard, and the answer is one case, not a class. A truncation
  dropping a rotation that has later updates is refused by chain replay, because those updates name
  the successor authority the truncation removed; a truncation dropping policy updates is refused by
  CBI42's floor. Both directions have a named test, and the residual case — a chain truncated at a
  trailing rotation, with this guard deleted — is stated rather than implied away.
- **Generation zero is not an unconstrained floor.** It must name the pin, because CBI38 admits an
  empty checkpoint only under a floor that does. Without that rule a stored zero under some other
  authority would be a floor no checkpoint could ever satisfy, so it is refused on decode and on
  retention, and the refusal is a pin mismatch rather than a regression.
- **An equal generation under a different active authority is a fork.** It is refused as a
  regression, for CBI42's reason at one level up: the floor would stop recognising the chain it was
  retained from.
- **A cycle that applies several rotations must retain each in turn.** Retaining only the last would
  leave a crash between two publications behind a floor that describes neither, so the handoff is per
  applied rotation and the contract states the prefix property over the whole cycle rather than an
  end-state equality.

## What the phase deliberately does not decide

The cycle is a call, not a schedule: nothing here decides when a host runs it, and no state survives
the process except the floor. Two hosts polling one registry are outside the claim, as CBI38's
one-writer bound already is. The integrity tag is CBI42's, with CBI42's limit unchanged — it detects
corruption, not an adversary who can write the file and recompute it.

## Residual limits

Custody in a domain the checkpoint's writer cannot reach remains the named boundary, as it was for
CBI42; a platform rollback anchor, a sealed key, or an attested counter is deployment work rather
than software work. Cross-process ownership of a rotation cycle, an offline grace for rotation
specifically, and durable scheduling that survives the process are separate boundaries. The next
bounded implementation boundary is a host-owned cadence that runs this cycle alongside CBI47's,
without either one's failure silently standing for the other's.
