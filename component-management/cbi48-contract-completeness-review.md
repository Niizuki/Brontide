# CBI48 contract-completeness review

Date: 2026-08-05

Scope: absence review of durable provider-trust cadence resumption, separate from C1-C8 conformance.

## Findings and dispositions

1. **The cadence journal cannot make CBI41, CBI46, and its own cursor one transaction.** A process
   can publish policy, advance the recovery floor, retire a provider, or remove staged content and
   then die before committing the cycle observation. Disposition: persist in-flight before invoking
   the cycle and report recovery as indeterminate. CBI48 deliberately makes no exactly-once claim.

2. **A failed commit after a returned cycle is still indeterminate.** The caller may know the result
   in memory, but recovery cannot distinguish that path from a crash during the delegate.
   Disposition: a failed journal write leaves the in-flight image intact and never silently advances
   the cursor. Reconciliation is required at the next open.

3. **A delay has a different recovery boundary from a cycle.** Repeating an interrupted delay does
   not repeat trust effects, while repeating an interrupted cycle may. A crash after a delay returns
   but before its completion is persisted can wait again; a crash after the ready image is persisted
   does not. Disposition: waiting and ready are distinct durable phases, and the prepared instant is
   recorded before the next cycle begins.

4. **Cancellation can race the end of a gap.** Cancellation observed before a completed gap is
   persisted leaves the waiting bytes unchanged. Once ready is persisted, the gap is complete even
   if cancellation arrives immediately afterward. Disposition: describe and test that exact boundary
   rather than claiming cancellation rolls back elapsed time.

5. **Retry is authorization to replay, not proof that replay is safe.** The journal cannot determine
   whether an interrupted CBI46 sweep already withdrew one or more members. Disposition: only an
   explicit reconciliation decision exposes retry, and it repeats the same index and instant.
   Evidence and policy supporting that decision remain the next host boundary.

6. **Abandonment can leave a completed gap without a cycle observation.** That would violate CBI47's
   ordinary completed-run property, but hiding the gap would erase a durable fact about the attempted
   next cycle. Disposition: the abandoned terminal state explicitly permits this one shape and
   retains the attempted index and instant through the snapshot.

7. **Multiple journal owners can overwrite one another.** The object serializes its own calls, but
   two objects or processes opened on the same path do not share that lock. Disposition: one host
   owns and serializes a run. CBI48 is not a file lease, distributed lock, fencing-token protocol, or
   multi-process scheduler; adding one requires an ownership capability rather than an incidental
   lock inside this journal.

8. **The integrity tag is not custody.** A writer with path access can replace the JSON record and
   its SHA-256 tag. Disposition: the tag detects accidental corruption and truncation only. Secure
   or rollback-resistant custody remains a privileged boundary, as it does for CBI42.

9. **Terminal state does not decide a provider restart.** A stopped, canceled, complete, or
   abandoned cadence says what happened to the run, not whether withdrawn providers should restart
   or how long unavailable policy may leave service running. Disposition: reopening terminal state
   is effect-free. Offline/reconciliation and provider restart policy remain separate work.

## Result

CBI48 is complete for one bounded, single-owner, host-local durable run. Its material result is the
indeterminate boundary: durability can prevent replay of committed cycles and expose an interrupted
attempt, but it cannot infer or atomically absorb external trust effects. Offline/reconciliation
policy and provider restart policy are the next host work; cross-process ownership, endpoint and key
rotation, privileged floor custody, and production isolation remain separate security boundaries.
