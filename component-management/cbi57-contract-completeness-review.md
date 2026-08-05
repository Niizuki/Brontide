# CBI57 contract-completeness review

Date: 2026-08-05

Status: complete

This review asks what the CBI57 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to the written C1-C8 requirements.

## Findings closed in the contract

- **Where a successor is retained is decided by what has to re-verify it.** CBI56 keeps its endpoint
  successor in a separate anchor, and repeating that shape here looked natural. It does not work: the
  CBI38 checkpoint replays its whole retained chain on every start, so a rotation recorded beside the
  chain would leave recovery verifying the predecessor's updates against a key that did not sign them
  — or trusting them unverified, which is the one thing the replay exists to prevent. The boundary
  states the rotation as a link in the same chain, and C4 makes the replay verify each update against
  the authority in force at its own position.
- **A staged phase would have been carried over from CBI56 without a reason.** CBI56 stages a
  successor and confirms it with one live synchronization because possession of an endpoint key can
  only be shown by using it against a peer. An authority key's whole function is signing, so the
  successor's countersignature over the same manifest proves the same thing at the point of the
  statement. C2 requires it and C3 states that no staged, announced, or unconfirmed successor exists;
  a named test asserts the absence rather than leaving it implied.
- **A rotation must not read as a trust event.** CBI43's chain records the authority a launch was
  governed by, and CBI44 and CBI45 compare it. Had the verified snapshot begun naming the signing key,
  every serving member would have been retired by the next rotation — CBI44's finding that the
  *decision* rather than the snapshot identity is what matters, arriving one level up. C7 keeps the
  snapshot naming the pin and both roots run a serving member across a rotation plus an update signed
  by the successor.
- **A rotation does not occupy the policy sequence.** Giving it one was considered and declined: the
  CBI39 cursor is a policy sequence and identity, so a rotation that advanced the sequence would make
  the distribution endpoint answer for a transition it knows nothing about. The rotation instead names
  the chain point it applies at, which C3 refuses when it does not match, so ordering is verified
  rather than merely positional.
- **The retained record's format marker advances only when a rotation exists.** Always writing the new
  shape was simpler and was declined, because a host that never rotates would silently change the
  bytes of a record earlier evidence describes. C5 states both directions and both are tested,
  including a checkpoint written before this slice.
- **Publication precedes the live authority.** CBI41 records the opposite ordering for a floor — a
  floor cannot precede the thing it describes — and the difference is which way the failure falls: a
  live authority no checkpoint records is forgotten at the next recovery, while a published rotation
  the live registry has not yet applied is simply repeated. C3 states the order and a write failure is
  a tested path in both roots.

## Findings that changed a test rather than the contract

- The first draft of C7 compared the current policy snapshot before and after a rotation. Because a
  rotation does not touch the snapshot, both sides were the same object and no implementation could
  have failed it — CBI17's shape exactly. It was replaced by the assertion that the *next* snapshot,
  verified under the successor, still names the pin, which a wrong implementation moves. Deliberately
  breaking the comparison confirmed the replacement goes red.

## Residual limits

The contract does not address how rotation statements reach a host, rotation of the out-of-band pin
itself, remediation of a compromised predecessor — which can sign an alternative successor at its own
generation, refused only by a retained floor — un-rotation or revocation of a successor other than by
rotating forward again, or durable custody of the authority floor, which stays the boundary CBI42
named. CBI42's policy floor store is untouched and keeps its own custody.
