# CBI54 capability contract — cross-process provider restart ownership

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI53 persists one restart lineage but deliberately assumes one owner. CBI54 places a host-local
cross-process ownership boundary in front of that journal. A live lease holds an operating-system
file lock and names a caller-supplied owner and lease identity. Acquisition publishes a durable,
integrity-checked fencing epoch before the lease may drive CBI53 or CBI52.

This is one-host coordination over a shared filesystem. It is not a distributed lease, clock-based
expiry protocol, exactly-once executor, or recovery proof for an interrupted provider effect.

## Capabilities

### C1 — ownership is bound to one restart lineage, at one journal

Acquisition requires distinct owner and lease identifiers, the CBI53 run, occurrence, and staged
content identities, and the path of the journal being fenced. A durable ownership record established
for another lineage is refused without replacement.

Lineage identities do not identify a journal: two journals can carry the same run, occurrence, and
staged identity, and a copied journal file carries them by construction. The ownership path is
therefore derived from the journal path rather than chosen by the caller, and the journal publishes
its own resolved path so the pairing is compared rather than trusted. A lease drives only the journal
it was acquired for; presented with any other journal it is not current, and no CBI53 transition is
attempted.

Property: every accepted epoch belongs to exactly one durable restart lineage at exactly one journal
path, and no lease validates against a journal it was not acquired for.

### C2 — one operating-system owner excludes other processes

The owner holds an exclusive writer lock for the lease lifetime. A competing process cannot acquire
the same ownership path and receives `restart-ownership-busy` without changing the durable record.
Because the ownership path is derived from the journal path, two hosts coordinating one journal
cannot select two different lock files and exclude nobody; one journal has exactly one ownership
path by construction.

Property: at most one live process can hold a lease for an ownership path, and at most one live
process can hold a lease for a journal.

### C3 — every acquisition advances a durable fencing epoch

While holding the lock, acquisition integrity-checks the prior record, increments its positive
64-bit epoch, and atomically publishes the successor before returning the lease. Missing state starts
at epoch one; corrupt, invalid, overflowing, or unwritable state fails closed and releases the lock.

Property: every later accepted lease has an epoch greater than every earlier accepted lease.

### C4 — only the current live lease may drive recovery

The CBI54 coordinator validates the live handle and exact owner, lease, epoch, and lineage record
before calling CBI53. Validation failure leaves the CBI53 journal unchanged and never calls CBI52.

Property: every CBI53 transition made through CBI54 is fenced by the then-current durable epoch.

### C5 — released and superseded leases are stale

Release closes the operating-system lock and is idempotent. The released object cannot validate or
drive recovery. Reacquisition advances the epoch even when owner and lease identifiers are reused,
so an earlier epoch never becomes current again.

Property: no stale lease can regain authority without a new acquisition and higher epoch.

### C6 — process loss relinquishes exclusivity without erasing history

When an owner process exits, the operating system releases its lock. A later process can acquire,
observe the durable prior epoch, and publish the next epoch; it receives no claim that the prior
owner completed an in-flight CBI53 attempt.

Property: ownership recovery preserves fencing history and CBI53 interruption semantics separately.

### C7 — ownership inspection is bounded and fail closed

Inspection reads only the bounded integrity-tagged state and reports missing, current, corrupt, or
lineage-mismatch observations. It does not acquire ownership, alter bytes, infer liveness from the
last record, or authorize recovery.

Property: a durable owner record is evidence of the last fencing decision, never proof of a live owner.

### C8 — both roots execute one shared ownership model

Reference C# and Minimal F# independently execute shared vectors and report code, epoch, owner,
lease, and whether the lease is live. Each root also uses a real child process to prove that its held
lock excludes another process and that release restores acquisition.

Property: every shared vector produces the same portable observation in both roots.

## Contract-completeness review

The contract covers lineage binding, journal pairing, live exclusion, durable fencing, atomic
publication, corrupt state, failed publication, release, stale leases, reacquisition, process loss,
inspection, coordinator preflight, and shared observations. It deliberately does not cover network
filesystems with weak locking or rename guarantees, distributed consensus, lease expiry, hostile
state writers, process identity attestation, provider-effect reconciliation, endpoint rotation, or
privileged custody.

The pairing requirement in C1 and C2 was added after delivery, on the finding recorded in the
[CBI54 contract-completeness review](./cbi54-contract-completeness-review.md): the exclusion was
originally over a caller-chosen ownership path, so two hosts could fence one journal from two
different lock files, hold two independent epoch sequences, and exclude nobody. CBI69 reached the
same conclusion for the cadence lock. This is a behaviour and public-API change, not a restatement.

## Deliberate limits

CBI54 coordinates cooperating host processes that use the CBI54 coordinator. The lower-level CBI53
journal remains a separately usable single-owner primitive and is not retroactively presented as
cross-process safe.
