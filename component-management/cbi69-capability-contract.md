# CBI69 capability contract — cadence run supervision

Date: 2026-08-09

Status: implementation contract

## Boundary

CBI68 fenced the CBI48 cadence journal with an epoch published in the record, and named what a fence
does not do: it makes a written-past holder harmless, and it does not stop a second host from opening
a run the first is driving. CBI69 supplies that exclusion. A supervisor holds a live operating-system
lock beside the journal for as long as it drives the run, and a cadence advances only while its
supervision is live.

Reading CBI68's limit in order to close it found two things it does not say.

**The fence's detection point is behind the effect.** A cadence writes after its cycle has run, so a
competitor that opens the journal mid-cycle and reconciles the in-flight attempt takes the run while
the first holder's cycle is still executing. The cycle happens, the commit is refused, and the record
keeps nothing of it. A named test in each root runs exactly that and requires the loss; the same
scenario under a supervision leaves the competitor refused before it reaches the record at all.

**A fenced holder does not alternate.** CBI68's residual limits say two holders that interleave writes
"fence each other alternately rather than one winning permanently". They do not: a refused transition
leaves the refused holder's epoch where it was, so the loser stays behind for good while the winner
keeps writing. Only a host that *reopens* rejoins, which is a decision it has to make and has no
reason to. The unsupervised outcome is therefore a silent permanent transfer rather than contention a
host would notice, which is a worse thing to leave unsupervised than the sentence implies. Both roots
pin it with a named test. CBI68 is not wrong about anything it enforces, and its text stands as
written; this is the sixth stated limit in this programme that described how something was called
rather than a rule anything applied.

This is one-host coordination over a shared filesystem, as CBI54 is. It is not a distributed lease,
a lease with a lifetime, an expiry protocol, a supervisor that starts or stops hosts, or a policy
about how many hosts a deployment may run.

## Capabilities

### C1 — one live supervisor excludes every other

Acquisition opens a lock beside the journal and holds it for the supervision's lifetime. A second
acquisition — in this process or another one — answers `cadence-supervision-busy`, returns no
supervision, and changes nothing about the record. Each root proves the operating system is doing the
excluding with a real second process, not a field the acquiring process happens to hold.

Property: at most one live supervision exists for a journal path, and a refused acquisition leaves the
journal's bytes unchanged.

### C2 — supervision excludes writers without claiming the run

Acquiring reads and writes no part of the record, so a run can be supervised before it is established
and CBI68's rule that ownership is claimed by writing is untouched. The lock says who may drive; the
epoch still says who last wrote.

Property: acquiring and releasing supervision move no journal's epoch and change no journal's bytes.

### C3 — the journal needs no second durable record

CBI54 publishes a durable fencing epoch beside its lock because CBI53 has none. This journal already
carries one, so this slice adds a lock and no state at all: the lock file is never read and never
written, and a supervisor that finds bytes in it is unaffected by them. A second record of a fact the
first record already holds is a thing that can disagree with it, which is CBI42's argument for the
policy floor and CBI65's for the availability baseline.

Property: no supervision reads or writes any file but the lock it holds open.

### C4 — a released or lost supervisor drives nothing

Release closes the lock and is idempotent. A released supervision refuses to advance the cadence with
`cadence-supervision-required`, and the cycle does not run. Process exit releases the lock, and the
next supervisor finds the record exactly as it was — including an attempt left in flight, which stays
CBI48's reconciliation to make rather than something acquiring resolves.

Property: no released supervision advances a cadence, and no acquisition changes a run's phase.

### C5 — the lock and the fence cover different holders

Neither replaces the other, and each root runs both directions. A holder the lock never excluded — one
that opened the journal without asking for supervision — is still refused
`durable-cadence-owner-superseded` at its next write, because the fence is a property of the record
rather than of this slice. A holder the fence cannot catch in time is the competitor of the boundary
above, and that is what the lock excludes.

Property: this slice adds no code to the journal's write path and removes none.

### C6 — supervision is bound to the run and path it names

The exclusion is over a path, so a supervision paired with a journal at another path, or with a
journal holding another run, would gate a cadence it excludes nobody from. Both are refused before the
cycle runs. The lock path is derived from the journal path so two supervisors cannot pick different
ones and both succeed.

Property: every advance made through the supervised coordinator is covered by a live lock over that
journal's own path.

### C7 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report each step's code, the
epoch the record reaches, and the cycles and phase it retains. Two of the vectors are the same
scenario with and without a supervision, which is what makes the difference the fixture's answer
rather than a comment.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

**Supervision is opt-in, and cooperating hosts are what it coordinates.** A host that opens the
journal without acquiring is not excluded by a lock it never asked for; it is caught by the fence, at
its next write, with everything that implies about when. This is CBI54's limit in the same words — the
lower-level journal remains a separately usable single-owner primitive and is not retroactively
presented as cross-process safe — and it is why C5 exists rather than being folded into C1.

**Acquiring before opening is the caller's ordering to get right, and nothing checks it.** A host that
opens first and acquires afterwards leaves the window the lock exists to close; what covers it is the
fence, which refuses that host's first write if anything happened in between. The two guards compose
here rather than either being sufficient.

**The lock file is not removed on release**, because deleting it would race a supervisor that has
already opened it. It carries nothing, so what is left behind is an empty file rather than state.

**A lock is not a lease.** Nothing expires, nothing is renewed, and a supervisor that stops driving
without releasing holds the run until its process exits. Detecting a host that is alive and idle is
not something a file lock can do, and this slice does not pretend otherwise.

CBI42's custody limit is unchanged: the journal's integrity tag detects corruption rather than an
adversary who can write the file and recompute it, and a lock over a path an adversary can also write
is no stronger. No other durable store in the programme gains a supervisor in this slice.
