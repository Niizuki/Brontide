# CBI69 contract-completeness review

Date: 2026-08-09

Status: complete

This review asks what the CBI69 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **CBI68 named the boundary and did not say what crossing it costs.** "It does not stop a second host
  from opening a run the first is driving" reads as contention. What it actually costs is an executed
  cycle: a cadence writes after its cycle runs, so a competitor that opens mid-cycle and reconciles the
  in-flight attempt takes the run while the effects are still happening, and the record keeps nothing
  of them. The contract states the ordering rather than the sentence, and a named test in each root
  runs the scenario both ways — once unsupervised, where the cycle is lost, and once under a lock,
  where the competitor never reaches the record.
- **"Fence each other alternately" is not what the fence does.** A refused transition leaves the
  refused holder's epoch unchanged, so the loser is out permanently and the winner keeps writing;
  rejoining requires reopening, which nothing prompts a host to do. Alternation would at least be
  visible from both sides. A silent permanent transfer is not, which strengthens rather than weakens
  the case for the exclusion. Pinned in both roots. CBI68 enforces nothing incorrectly and its text
  stands; this is the sixth stated limit in the programme that described how something was called.
- **Supervision claims nothing, and that had to be decided rather than assumed.** CBI68's C2 was
  corrected by CBI48's own tests into "opening observes", and a lock that quietly wrote an owner into
  the record would have undone that from outside. Acquisition therefore reads and writes no part of
  the record, which also lets a run be supervised before it exists — the ordering a host needs if the
  lock is to cover establishment at all.
- **The obvious design adds a durable record, and it should not.** CBI54 publishes an epoch beside its
  lock because CBI53 has none; copying that shape here would put a second owner-record next to one the
  journal already carries, which is the design CBI42 argues against for the policy floor and CBI65 for
  the availability baseline. The slice adds a lock and no state, and C3's test plants bytes in the lock
  file to show nothing reads them and nothing overwrites them.
- **What the lock is bound to is a decision, not plumbing.** The exclusion is over a path, so a
  supervision handed a journal at a different path — or one holding a different run — would gate a
  cadence it excludes nobody from. Both refuse. The journal now publishes its own resolved path, which
  is what makes the check possible; without it the pairing would have been trusted.
- **Which guard catches whom is stated per holder.** Neither guard subsumes the other: the lock cannot
  exclude a holder that never asked for supervision, and the fence cannot catch a competitor before
  the cycle it is racing. The contract assigns each case rather than presenting the lock as a
  replacement for the fence.

## What the phase deliberately does not decide

How many hosts a deployment may run, and which host should be driving. The lock answers "not two at
once, here, now"; it does not elect, start, stop, or prefer a host, and nothing in the slice knows
whether a second host *should* have been started.

Whether a supervisor that is alive and idle is holding a run it has abandoned. That is a liveness
question, and a file lock cannot answer it.

## Residual limits

**Supervision is opt-in and coordinates cooperating hosts**, which is CBI54's limit in the same words.
A host that opens the journal without acquiring is caught by the fence at its next write rather than
excluded, with everything the first finding says about when that is.

**Acquire-before-open is the caller's ordering and nothing checks it.** A host that inverts it leaves
the window the lock exists to close, and the fence is what covers the inversion.

**A lock is not a lease**: nothing expires or is renewed, and a stalled supervisor holds the run until
its process exits. The lock file is left behind on release, because deleting it would race a supervisor
that has already opened it; it carries nothing, so an empty file is what remains.

CBI42's custody limit is unchanged and a lock does not extend it: an adversary who can write the
journal and recompute its tag can write the lock path too. No other durable store in the programme
gains a supervisor in this slice — CBI53's restart journal already has CBI54's, and CBI67's
stop-attribution store, CBI38's checkpoint, and CBI42's floor store still have none.
