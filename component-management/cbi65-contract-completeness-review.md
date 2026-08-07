# CBI65 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI65 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C8.

## Findings closed in the contract

- **The item was reachable and needed no new record, which is not what a "durable" boundary usually
  means.** CBI64 named this boundary as deriving a baseline from CBI48's committed observations, and
  reading CBI48 rather than the pointer showed the journal has recorded each cycle's instant and code
  since it was written. The slice therefore adds a classification to an existing vocabulary and no
  storage at all. That is worth stating because the opposite reading — retain the baseline beside the
  journal — is the obvious one and is the design CBI42 argues against for the policy floor: a second
  record of a fact the first record already holds is a thing that can disagree with it.
- **The question the derivation asks is not one the vocabulary answered.** `Continues` says whether a
  cadence may go on; nothing said whether a cycle established current policy. Putting the answer in
  the derivation rather than the vocabulary would have reproduced CBI62's defect one consumer over: a
  code a cycle can produce and a consumer cannot classify. The classification is therefore beside
  `Continues`, and a shared fixture section pins the answer for every code rather than only for the
  codes today's vectors exercise.
- **`provider-trust-cycle-stopped` cannot be classified, and that is a property of the cycle rather
  than an omission.** `ProviderServingTrustCycle` returns it both for a poll that was not current and
  for a current poll whose sweep failed, so the record does not say whether that cycle established
  anything. Choosing either answer would be a guess in the place a guess is least visible. It is
  refused, and the refusal outranks any establishing cycle behind it — a baseline computed from the
  observations before the unclassifiable one would be confidently wrong about everything after it,
  which a second vector pins.
- **The refusal is unreachable through CBI48 and reachable through the derivation, so it is neither
  manufactured nor unpinned.** CBI48 terminates a run on a non-continuing code in the write that
  commits it, so no journal it wrote can hold one in front of a later cycle. The derivation takes a
  snapshot, which a caller can construct, so the input is real. That CBI48 behaves this way is a claim
  about a dependency rather than about this slice, so C6 probes it: every continuing code keeps the
  run going and each unanswered one ends it.
- **A terminal journal is a source, not a thing to reject.** The tempting rule is that a baseline
  belongs to the run that produced it, and it would make a host that shut down cleanly stop service at
  its first outage after restarting. CBI49 anchors the deadline in absolute time, so an old baseline
  is already expired and needs no rejection; the run identity is therefore not compared and the
  contract says why rather than leaving the permission to look like an oversight.
- **The freshness guard a reader expects is absent on purpose.** A baseline later than the evaluating
  instant is already `offline-observation-invalid` under CBI49, so adding a check here would be a
  second refusal for one condition — the shape CBI44 declined for the same reason. The contract states
  the absence.
- **The composed failure has a direction, and the wrong answer is the plausible one.** Seeding a
  resumed cadence with its own restart instant is the fix a reader reaches for, and it renews grace on
  every restart, so a host crash-looping inside an outage would serve indefinitely past a deadline
  that never arrives. C7 runs three successors over the same outage at the same instant — derived,
  none, and restart-anchored — so the test distinguishes the correct answer from the plausible one
  rather than merely confirming it.

## What the phase deliberately does not decide

Whether a host should resume at all remains CBI63's question, answered from the cursor an interrupted
attempt recorded. The two derivations read the same journal and do not overlap: one asks what the
interrupted attempt did, the other what the committed record says about availability.

## Residual limits

C1's central property is a compile-time one: the derivation is handed a snapshot rather than a
journal, so it has nothing to write to, and no runtime test can reach a defect the type system
prevents. The test pins the nearest observable thing — the durable bytes are unchanged across a
derivation, including a refused one — and this is recorded rather than presented as coverage it is
not.

The derivation trusts the journal exactly as far as CBI48 does: its tag detects accidental damage, not
a writer that can replace the record and recompute it. Nothing here proves a provider was serving at
the instant the record names, and nothing reads a second host's journal. A cadence still evaluates at
its own interval, so CBI64's observation lag is unchanged.
