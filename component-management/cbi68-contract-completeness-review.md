# CBI68 contract-completeness review

Date: 2026-08-09

Status: complete

This review asks what the CBI68 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **Six slices named a boundary and none said what crossing it cost.** CBI48 declares one process and
  one writer, and CBI62, CBI63, CBI64, CBI65 and CBI67 each repeat that cross-process ownership
  remains separate. What none of them records is that the bound is unenforced *and* that violating it
  destroys data rather than producing an error: a holder whose copy is behind writes that copy back
  and a committed cycle is gone from the record with nothing reporting it. The defect was pinned by a
  failing test before the slice was designed, and the failure named the mechanism — a reopened journal
  held zero cycles where one had been committed.
- **The obvious reading of "ownership" was wrong, and existing tests are what said so.** Claiming the
  run at `Open` is the first design anyone writes, and three CBI48 tests refuse it: C3 opens a journal
  from inside a running cycle purely to observe the in-flight phase and then expects the driving
  holder to commit, and C5 and C7 compare the durable bytes across a recovery and require them
  unchanged. A slice that takes a run away from a host in order to look at it breaks the component it
  is protecting. Ownership is therefore claimed by writing, and the contract states that as the
  capability's shape rather than as an implementation detail.
- **The correction made the migration disappear rather than easier.** Under claim-on-open a record
  written before this slice needed an adoption rule, and the contract had one. Under claim-on-write it
  needs none: the holder sees epoch 0 and its first write claims the run at 1. A rule that is no longer
  necessary is better than a rule that works, and the review records which design removed it.
- **The guard precedes the phase preconditions, and that ordering is a decision.** Those preconditions
  are judged from in-memory state, and a superseded holder's state is known to be out of date. A vector
  caught this: with the guard only at the write, a fenced holder was told `durable-cadence-cycle-not-
  started`, naming a protocol error it had not made instead of the run it had lost. A named test walks
  every transition and requires all of them to report the lost run, so a later transition cannot be
  added without the guard.
- **An unreadable record is left alone rather than reclassified.** The first draft refused it as
  damage, which changed an outcome CBI48 already defines — its own C3 replaces the journal with a
  directory and requires `durable-cadence-write-failed`. Only a record that can be read can prove
  supersession, and refusing what cannot be read would have added a second meaning to a path that
  already had one. The contract states that this slice introduces one code and changes none.
- **Observation stays ungated on purpose.** A superseded holder still needs to say what it last knew,
  because that is what a host reports when it stops, and CBI63's reconciliation reads a snapshot. What
  it cannot do is make that view durable, which is the whole of the restriction.

## What the phase deliberately does not decide

How many hosts a deployment may run. The fence makes a written-past holder harmless; it does not
choose which host should be driving a run, and it does not prevent a second one from starting.

## Residual limits

**This is a fence, not a lock.** Two holders that interleave writes fence each other alternately rather
than one winning permanently — louder than the silent erasure it replaces, quieter than a lock would
be, and stated rather than left to be discovered. CBI54 pairs its epoch with a live operating-system
file lock for exactly that exclusion, and pairing one here is the remaining supervision boundary.

The epoch is a counter in a record whose integrity tag detects corruption rather than an adversary who
can rewrite the file and recompute it, which is CBI42's limit and unchanged. No other durable store in
the programme gains an owner in this slice: CBI53's restart journal already has CBI54's ownership, and
CBI67's stop-attribution store, CBI38's checkpoint, and CBI42's floor store do not.
