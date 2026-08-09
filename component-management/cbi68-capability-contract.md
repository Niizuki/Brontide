# CBI68 capability contract — cadence run ownership

Date: 2026-08-09

Status: implementation contract

## Boundary

CBI48 states that its journal is bound to one process and one writer, and every slice built on it —
CBI62, CBI63, CBI64, CBI65 — repeats that cross-process ownership remains separate. None of them says
what happens when the bound is broken, and the answer is worse than a refusal.

`Open` reads the record into memory and takes no lock and no fence. Two holders of one journal each
keep their own copy of the state and each write the whole record back, so a holder that has not seen
another's progress **erases it**. That was pinned by a failing test before this slice was designed: a
holder that opened before a cycle was committed, then acted, left a reopened journal holding zero
cycles where one had been committed, and its transition answered `durable-cadence-cycle-started`. A
holder superseded while its own memory was current wrote over the phase of a run it no longer owned.

CBI68 fences both. The mechanism is CBI54's, one component over: an epoch published in the record
itself, so a holder the record has moved past is refused instead of writing.

This is not a lock, a supervisor, a lease with a lifetime, or a claim about which host *should* own a
run.

## Capabilities

### C1 — every write advances the epoch

`Establish` writes epoch 1, so a run is owned from the moment it exists rather than from its first
transition. Every transition thereafter writes the next epoch. The epoch is a write counter, which is
what makes a holder that has been written past detectable at all.

Property: the epoch of a record equals the number of writes made to it.

### C2 — opening observes, and does not take the run

`Open` reads and writes nothing. Ownership is claimed by writing, so a host may inspect a run as often
as it likes without disturbing the holder driving it.

This is the capability's shape and it was **corrected by CBI48's own evidence rather than chosen**. The
first design claimed ownership at `Open`, which is the reading of "ownership" that comes to mind first.
Three existing tests refused it: C3 opens a journal from inside a running cycle purely to observe the
in-flight state and then expects the driving holder to commit, and C5 and C7 compare the durable bytes
across a recovery and require them unchanged. A slice that takes a run away from a host in order to
look at it breaks the component it is trying to protect.

Property: no `Open` changes the durable bytes, and no `Open` refuses a transition the holder would
otherwise have made.

### C3 — a superseded holder writes nothing

Every transition compares the epoch it last saw or wrote with the epoch in the record, before it reads
its own phase. A holder the record has moved past is refused as `durable-cadence-owner-superseded`,
and the record is left exactly as it was.

The comparison precedes the phase preconditions deliberately. Those are judged from in-memory state,
and a superseded holder's state is known to be out of date, so reporting one of them would name a
protocol error the holder did not make instead of the run it lost. A named test walks every transition
a superseded holder can attempt and requires all of them to say so, which is what keeps a later
transition from being added without the guard.

Property: after any refused transition, the durable bytes are unchanged.

### C4 — a record written before this slice needs no adoption rule

Such a record deserializes with epoch 0. Because opening observes rather than claims, the holder simply
sees 0 and its first write claims the run at 1. No format marker moves, no host loses a run it is in
the middle of, and there is no special case — which the first design would have needed, and which is
the second thing the corrected shape made unnecessary rather than easier.

Property: a record carrying no epoch opens, transitions, and reaches epoch 1 on its first write.

### C5 — an unreadable record keeps the outcome CBI48 already defines

Only a record the holder can read can prove it was superseded. One that cannot be read is left to the
write path that already handles it: a journal deleted or replaced by a directory has answered
`durable-cadence-write-failed` since CBI48, and reclassifying that as damage would change an outcome
this slice has no reason to touch. It is also no weaker a guard than the integrity tag it failed,
which is CBI42's limit either way.

Property: this slice introduces one new code and changes no code CBI48 already produced.

### C6 — ownership gates transitions, not observation

`Snapshot` remains the holder's own view and is not gated. A superseded holder can still describe what
it last knew, which is what a host needs in order to report why it stopped; what it cannot do is make
that view durable. CBI63's reconciliation reads a snapshot and is unaffected.

Property: no read-only member of the journal can fail because of ownership.

### C7 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report each transition code,
the epoch the record reaches, and the cycles and phase it retains.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

**This is a fence, not a lock.** It makes a written-past holder harmless; it does not stop a second
host from opening a run the first is driving, and whichever writes next owns it. CBI54 pairs its epoch
with a live operating-system file lock for exactly that exclusion, and pairing one here is the
remaining supervision boundary — the fence is what the pinned defect required, and an exclusion policy
is a separate decision about how many hosts a deployment may run.

Two holders that interleave writes will therefore fence each other alternately rather than one winning
permanently. That is louder than the silent erasure it replaces and quieter than a lock would be, and
it is stated rather than left to be discovered.

The epoch is a counter in a record whose integrity tag detects corruption rather than an adversary who
can rewrite the file and recompute it, which is CBI42's limit and unchanged. No other durable store in
the programme gains an owner in this slice.
