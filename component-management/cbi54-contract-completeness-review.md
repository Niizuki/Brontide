# CBI54 contract-completeness review

Date: 2026-08-10

Status: complete

Backfilled: this phase boundary was originally recorded only as an inline section of the
[CBI54 capability contract](./cbi54-capability-contract.md). The review below was performed against
the merged slice at the later date and is the standalone absence audit the practice calls for.

## Review question

CBI54 puts a cross-process boundary in front of a single-owner journal. What must be true of the
pairing between the lock and the journal it protects, and does the C1-C8 contract force that pairing
to hold?

## Findings and dispositions

1. **Nothing binds the ownership path to the journal it fences.** C2's exclusion is over an ownership
   path: at most one live process holds a lease for *that path*. The journal is a separate object at
   a separate caller-supplied path, and the coordinator receives the two independently. C1 and C4 bind
   them by *identity* — run, occurrence, and staged-content — never by location. Two hosts that
   acquire ownership at two different ownership paths for one journal therefore both hold valid
   leases, both validate the lineage successfully, and both drive CBI53. Worse than losing the
   exclusion, they advance two independent epoch sequences, so C4's property — every transition is
   fenced by the then-current durable epoch — is true of each host separately and means nothing
   between them. The exclusion is real only under a deployment convention that derives one ownership
   path per journal, and no capability states that convention.

   Disposition: **closed by change**, and it is the material result of this review. CBI69 identifies
   this exact defect class for the cadence lock; the remedy applied here is stronger, because CBI53's
   journal has no epoch of its own and so offers no backstop the way CBI68's does. Rather than
   detecting a mispairing, the ownership path is now *derived* from the journal path, which makes two
   hosts on one journal collide on one lock file by construction. The journal additionally publishes
   its resolved path, and the lease compares it, which catches a lease pointed at a different journal
   that the identity comparison alone accepts — the copied-journal case. C1 and C2 now state the
   pairing, and a named test in each root covers it.

   Worth recording: CBI55's own test fixtures acquired ownership at `restart.owner` while driving a
   journal at `restart.journal`. Three of them went red on the change. The hazard was not theoretical
   — the mispairing was already present in the estate, in the slice built directly on top of this one.

2. **A durable record proves a decision, never a live owner — and the contract says so.** C7 states
   this directly and refuses to infer liveness from the last record. Disposition: closed, and
   unusually well stated. This is the finding most cross-process designs get wrong and it is pinned
   as a property rather than left to prose.

3. **Nothing expires, so a stalled owner holds forever.** C2 gives the lock the lease's lifetime and
   the boundary section excludes clock-based expiry as a non-goal. A process that is alive but wedged
   holds its lineage until it exits, and C6 only helps once the operating system releases the lock.
   Disposition: closed as a stated non-goal, with the reviewer's note that "lease" is the wrong word
   for what this is — CBI69 later says plainly that a lock is not a lease, and CBI54's own vocabulary
   invites the confusion it then has to exclude in prose.

4. **Owner and lease identifiers are caller-supplied and unauthenticated.** C1 requires them distinct
   and C5 makes reacquisition advance the epoch even when both are reused, which handles honest
   reuse. Nothing prevents a second process from claiming another's owner identity. Disposition:
   closed by the stated scope — CBI54 coordinates *cooperating* host processes, and the contract's
   own limits exclude process identity attestation and hostile state writers. The epoch, not the
   owner name, is what carries authority, which is the right choice.

5. **Fail-closed on corrupt state is stated; recovery from it is not.** C3 fails closed and releases
   the lock when prior state is corrupt, invalid, overflowing, or unwritable, and C7 reports corrupt
   on inspection. No transition repairs or replaces a corrupt ownership record, so a lineage whose
   record is damaged cannot be owned again. Disposition: correct for a fencing record — silently
   rebuilding one would reset the epoch and let a stale lease regain authority, which C5 exists to
   prevent. Recovery is deliberately an operator action outside the slice. Recorded because C3
   describes the refusal and never says the state is terminal.

6. **Epoch overflow is handled at a boundary no deployment will reach.** C3 fails closed on
   overflowing a positive 64-bit epoch. Disposition: closed and correct; noted only because the same
   care was not taken with the pairing in finding 1, where the reachable hazard lives.

## Result

The reviewed contract covers lineage binding by identity, live exclusion, durable fencing, atomic
publication, corrupt and unwritable state, release, stale and superseded leases, reacquisition,
process loss, bounded inspection, coordinator preflight, and shared cross-root observations including
a real child process per root.

Findings 2 through 6 are closed by the contract's boundary or by its stated scope. Finding 1 was the
material result of this review and is **closed by change**: the ownership path is now derived from
the journal path, the journal publishes that path, the lease compares it, C1 and C2 state the
requirement, and each root has a named test that was observed failing before the fix.

Network filesystems with weak locking or rename guarantees, distributed consensus, lease expiry,
hostile state writers, process identity attestation, provider-effect reconciliation, endpoint
rotation, and privileged custody remain explicit non-goals.
