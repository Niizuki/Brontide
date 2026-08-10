# CBI50 contract-completeness review

Date: 2026-08-10

Status: complete

Backfilled: this phase boundary was originally recorded only as an inline section of the
[CBI50 capability contract](./cbi50-capability-contract.md). The review below was performed against
the merged slice at the later date and is the standalone absence audit the practice calls for; the
inline paragraph remains as the contract author's own statement of scope.

## Review question

What can happen to an exact host-supplied serving set between the host observing it and CBI50
finishing with it, that the C1-C8 contract does not force the caller to account for?

## Findings and dispositions

1. **The serving set is a snapshot, and the contract never says it is still true.** C1 validates that
   non-empty members are serving at preflight, then passes that count to CBI49. A member that exits on
   its own between preflight and its turn in the sweep is neither invalid input nor a retirement
   failure. Disposition: C5's guarantee is that every admitted member receives one observation, so an
   already-stopped member is observed rather than skipped, and the aggregate is decided by whether its
   provider can be confirmed stopped — which for an exited process it can. The policy decision keeps
   the preflight count, because C1's property pins the count that was passed, not a recount.

2. **An oversize serving set has no enforcement path at all.** C7 refuses more than 64 members with
   `offline-enforcement-invalid` and zero effect. Batching is the obvious response and C1 forbids it:
   the whole set determines the count passed to CBI49, so two calls of 40 would each present a
   different, smaller availability picture than the host actually has. Disposition: 64 is a bound on
   what this slice claims, not a host capacity limit. A host serving more than 64 members is outside
   CBI50's claim and the refusal is honest about that. Raising the bound is a contract change, not a
   caller workaround.

3. **A stop decision over an empty admitted set has no stated aggregate.** C2 gives `offline-idle` an
   empty set its own code, but C3's three stop codes are described only in terms of reaching every
   member. Disposition: the stop conditions hold vacuously over zero members, so the aggregate is
   `offline-enforcement-stopped` with an empty observation list. This is the same shape as C5's
   completion rule and needs no separate code; recorded here because the contract states it nowhere.

4. **Terminating a provider does not obviously release its removal lease.** C6 keeps staged artifacts
   deliberately, and CBI52/C3 says a restart takes a *new* removal lease when it relaunches. Nothing
   in CBI50 says what becomes of the stopped member's existing lease. A lease that outlives its
   provider pins content that C6 only promised to retain, not to hold indefinitely. Disposition:
   none available from the text. `lease` is CBI52 contract vocabulary that this review could not
   locate as a named mechanism in either root's restart-enforcement source, so whether a stop
   releases one, and whether "new lease" means a second lease or a reacquired one, cannot be settled
   from the contract or the code as written. Recorded as an open item against CBI52's vocabulary
   rather than dispositioned; it is the one finding here that needs an answer from an implementer.

5. **Identity order is deterministic and is not dependency order.** C4 processes members in ordinal
   `OccurrenceId` order so that two roots agree. For members that depend on each other inside a CM3
   activation group, that order can stop a dependency before its dependent. Disposition: an
   availability stop is not a lifecycle teardown. CM4 owns ordered Release, and CBI50 deliberately
   does not execute one — which is also why C3 terminates the concrete provider even when graceful
   retirement fails. Determinism across roots is the property being bought, and the cost is stated.

6. **"Every stop decision reaches every member" says nothing about reaching them promptly.** There is
   no bound on how long a member may take to retire, and C5 records failure per member without a
   timeout. Disposition: the call is sequential and caller-triggered, so the caller owns the wall
   clock; a member that never returns blocks the sweep. Bounded termination is not claimed and belongs
   with the process-tree termination the contract already excludes.

## Result

The reviewed contract covers the zero-member case, the grace boundary, invalid observations,
duplicate and unavailable members, deterministic order, per-member failure isolation, preflight
zero-effect, and artifact retention. Findings 1, 2, 3, 5, and 6 are closed by the contract's own
boundary once stated. Finding 4 is a genuine silence shared with CBI52 and remains **open**: it is
the only finding in this review that a contract edit alone cannot close.

Restart eligibility, retry timing, process-tree termination, concurrent mutation of the serving set,
and durable recording of the stop remain explicit non-goals owned by CBI51 through CBI54.
