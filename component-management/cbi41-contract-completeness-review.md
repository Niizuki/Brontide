# CBI41 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI41 poll cycle, separate from conformance tests. It asks what the
contract does *not* say, per capability.

## Findings and dispositions

1. **The contract never said what a retry is for, so every failure could have been retried.**
   Disposition: stated as a rule rather than a list. A retry changes the challenge, the cursor read,
   and the network; the retryable set is exactly the outcomes those three can change. The list is
   therefore derivable rather than remembered, and a future CBI39 code has an answer before anyone
   writes one.
2. **Backoff could have been a function of the attempt index.** Disposition: prevented and pinned.
   It is a function of consecutive failures, which progress resets, and a shared vector runs a
   failure, an application, and a failure so the reset is observed rather than described.
3. **A long budget could overflow the delay computation before the cap bounded it.** Disposition:
   prevented in both realizations by clamping at each multiplication step, and bounded by refusing a
   budget over 64 attempts, a multiplier over 16, and a maximum delay over one hour.
4. **The floor could have been retained before the checkpoint it describes.** Disposition: prevented,
   and observed rather than asserted — the sink reopens the checkpoint and both stacks record that
   the update the floor names is already durable. The ordering is the reverse of CBI38's own
   publish-before-advance, and the review that matters is why: CBI38 publishes early because a
   durable record ahead of a live claim is safe, while a floor ahead of publication claims state no
   checkpoint holds, which recovery reads as `policy-checkpoint-rollback-detected` — a refusal to
   open a checkpoint nothing rolled back. **A lagging floor under-detects for one update; a leading
   floor denies service outright, and only the second is unrecoverable without intervention.** The
   lag also self-heals: recovery issues a fresh floor from the replayed checkpoint.
5. **A failed handoff could have been treated as a failed update.** Disposition: refused. The update
   is already published and live, so the cycle neither undoes it nor continues past it, and the
   result carries the applied sequence with no matching retained sequence. A cancelled handoff
   produces the same outcome as a failing one, deliberately, because the outcome names the state the
   host is left in and cancellation does not change that state.
6. **A delay that fails for a reason other than cancellation had no stated behaviour.** Disposition:
   deliberate and asymmetric with the sink. It propagates: a host defect in the delay is not a
   distribution outcome, and unlike a sink failure it leaves no durable state the cycle owes anyone a
   report about.
7. **The cycle could have read a clock.** Disposition: refused. Its instant starts at the caller's
   and advances only through the injected delay, which a real host implements as a wait followed by a
   clock read, so even a zero gap advances. A host that returns a frozen instant eventually provokes
   freshness refusals, which are retryable and therefore bounded by the budget rather than endless.
8. **Jitter is absent.** Disposition: explicit, and it needs no contract change to add. The cycle
   asks for a duration and the host decides how to wait it, so a host wanting jitter implements it in
   its delay without moving the seam. Keeping it out is what lets the shared vectors pin an exact gap
   sequence across two independent realizations.
9. **The budget counts attempts, not elapsed time.** Disposition: explicit absence. There is no
   overall deadline; the caller's cancellation token is the only one, and a cycle's worst-case
   duration is the budget times the capped delay.
10. **An endpoint whose validity window is always wrong burns the whole budget.** Disposition:
    accepted and bounded. Treating a stale window as terminal instead would make a transient clock
    skew unrecoverable without host intervention, which is the worse failure.
11. **A second writer can advance the registry between attempts, and CBI38 says there is no second
    writer.** Disposition: recorded rather than resolved. CBI39 declares a superseded cursor, which
    is reachable only when something else advances the registry mid-attempt, while CBI38 bounds
    itself to one process and one writer — so the category exists in one slice and is excluded by
    the next. The shared vector provokes it by writing from the fake source, deliberately outside
    CBI38's bound, because a declared category with no reachable path is the defect PB6 found three
    of. What the poller does not do is claim the resulting advance: its applied and retained
    sequences name only its own, so a sequence it did not apply gets no floor from it.
12. **Nothing reads a retained floor back.** Disposition: explicit boundary. CBI38's `Open` already
    accepts a floor, and closing the loop would require durable custody of it — which CBI38
    explicitly declines to claim and which no slice yet provides.
13. **Nothing schedules the next cycle.** Disposition: explicit. One call performs one cycle and
    returns; timers, hosted workers, supervision, and offline policy are the host's.

## Result

The CBI41 contract is complete for one bounded, deterministic, host-driven poll cycle over CBI39 with
publication-ordered floor handoff. The next boundary should give the handed-off floor durable custody
that `Open` consumes on the following start, closing the loop this slice only hands off; endpoint and
authority key rotation, a platform rollback anchor, and a real scheduling host remain separate work.
