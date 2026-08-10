# CBI53 contract-completeness review

Date: 2026-08-10

Status: complete

Backfilled: this phase boundary was originally recorded only as an inline section of the
[CBI53 capability contract](./cbi53-capability-contract.md). The review below was performed against
the merged slice at the later date and is the standalone absence audit the practice calls for.

## Review question

CBI53 makes restart history durable and states plainly that it cannot atomically commit provider
effects alongside journal writes. What does that admitted gap oblige a host to decide that the C1-C8
contract does not make it decide?

## Findings and dispositions

1. **Indeterminate has no deadline and no decider.** C6 returns `durable-restart-indeterminate` on
   reopening an in-flight journal and offers `Retry` and `Abandon`. Nothing bounds how long a journal
   may sit in-flight, and nothing says who chooses between the two or on what evidence. A host that
   never reopens leaves a lineage in-flight indefinitely, holding its occurrence against any later
   attempt. Disposition: the choice is deliberately the host's, because the journal has no way to
   observe the effect it is uncertain about — that is C6's whole premise. What the contract does not
   say, and this review records, is that an in-flight lineage is *not* self-healing: no timeout
   converts it to abandoned, and the absence of a timeout is a decision rather than an oversight.
   CBI55 later supplies the evidence a successor needs to choose `Retry` responsibly.

2. **Atomic replace assumes a filesystem the contract never names.** C2's guarantee — every snapshot
   is the complete prior or complete successor state — rests on write-to-sibling plus atomic rename.
   CBI54, written days later, explicitly excludes "network filesystems with weak locking or rename
   guarantees". CBI53 makes the stronger structural claim on the weaker stated basis and inherits none
   of that exclusion, because it precedes it. Disposition: **open as text.** The assumption is
   correct and the exclusion is real; C2 should carry CBI54's filesystem limit rather than leaving a
   reader to find it one slice later. No code change is implied.

3. **The integrity tag is stated to detect accident, and the contract stops there.** C2 says the
   SHA-256 tag detects "accidental corruption and truncation". It does not claim to detect deliberate
   modification, and it cannot: an adversary who can write the journal can recompute the tag.
   Disposition: closed and correctly scoped. This is CBI42's custody limit in the same words, and
   CBI69 later confirms a lock does not extend it. Recorded because "SHA-256" reads as a security
   claim to a reader who skips the adjective.

4. **A lineage bound to staged identity has no path across a legitimate content change.** C1 binds
   one journal to one occurrence *and* staged-content identity, and refuses mismatches without
   mutation. When a publisher legitimately ships new content for the same occurrence, every refusal
   is correct and the lineage is stranded — the contract offers no supersede or retarget transition.
   Disposition: correct by design. A new staged identity is a different restart lineage, and reusing
   the journal would let attempt budget from old content constrain new content. The host establishes
   a new journal; the old one is abandoned through C6. Recorded because C1's refusals describe this
   only as a mismatch error, never as the normal upgrade path.

5. **Exhaustion is terminal with no operator path back.** C5 commits `durable-restart-exhausted` at
   the configured maximum and C7 makes terminal states idempotent — opening one returns the snapshot
   without evaluating policy. An operator who fixes the underlying fault has no transition that
   reopens the lineage. Disposition: closed by the same reasoning as finding 4 — recovery is a new
   lineage, not a reset of this one, which keeps the durable record an honest history rather than a
   mutable counter. The absence of a reset is what makes C5's property ("no lineage invokes more
   attempts than its durable maximum") true.

6. **Two single-flight mechanisms guard the same effect at different scopes.** C4 persists in-flight
   before invoking CBI52, and CBI52/C6 independently maintains its own in-process claim. The contract
   does not reconcile them or say which is authoritative when they disagree — for instance when the
   journal says in-flight and the CBI52 claim was released by a failed launch. Disposition: they
   answer different questions. The journal records that an attempt *was started* and survives the
   process; the claim prevents a second concurrent call inside one process and is deliberately
   released so a later policy-approved attempt may retry. Recorded because C4's ordering makes them
   look layered when they are orthogonal, and a reader can take the durable marker for a lock.

## Result

The reviewed contract covers identity binding, bounded history, delay and exhaustion, in-flight
ordering, write failure, corruption, interruption, retry, abandonment, success, and terminal
idempotence. Its central honesty — that a missing committed result is never proof that no effect
occurred — is stated as a property rather than left to a reader.

Findings 1, 3, 4, 5, and 6 are closed by the contract's boundary once stated. Finding 2 is **open as
text**: C2 should name the filesystem assumption CBI54 later names.

Coordinating two journals or processes, recovering a provider connection after host death, proving an
interrupted effect occurred, endpoint or key rotation, exhausted-content cleanup, and privileged
custody remain explicit non-goals owned by CBI54, CBI55, and the distribution, maintenance, and
security boundaries.
