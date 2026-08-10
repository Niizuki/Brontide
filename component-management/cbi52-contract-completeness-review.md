# CBI52 contract-completeness review

Date: 2026-08-10

Status: complete

Backfilled: this phase boundary was originally recorded only as an inline section of the
[CBI52 capability contract](./cbi52-capability-contract.md). The review below was performed against
the merged slice at the later date and is the standalone absence audit the practice calls for.

## Review question

CBI52 is the first slice in this run that performs provider effects. What can fail, or change
underneath it, between the eligibility recheck and a serving successor, that the C1-C8 contract does
not force it to account for?

## Findings and dispositions

1. **"Rechecked before effects" is an ordering, not an interval.** C1's property — every launch is
   preceded in the same call by one ready decision — pins that nothing launches on a decision made
   elsewhere. It does not bound what happens between the recheck and the launch, and CBI51/C2's
   current-cycle proof can go stale inside that window if the registry rotates. Disposition: same-call
   recheck is the strongest claim an in-process call can make without holding a registry lock, and
   the contract does not claim atomicity. The residual window is narrower than the alternative it
   replaces (a decision carried from a previous call) and is stated here rather than closed.

2. **C5 terminates the new provider and does not say what a failed termination means.** The
   fail-closed path is "the new provider is terminated and its lease released", written as though
   termination succeeds. CBI50/C5 faced the same case and answered it, distinguishing
   `offline-enforcement-cleanup-incomplete` from `offline-enforcement-incomplete`. CBI52 has no such
   split: its property says every failed reconstruction leaves no newly serving provider, which a
   process that refuses to die would falsify. Disposition: **open.** Either C5 needs the aggregate
   distinction CBI50 already makes, or its property needs weakening to what termination can actually
   guarantee. The asymmetry between two sibling slices written days apart is itself the finding.

3. **The removal lease is vocabulary without a located mechanism.** C3 says the store "takes a new
   removal lease" and C5 says a failed reconstruction releases it. This review could not find a
   correspondingly named construct in either root's restart-enforcement source, which leaves three
   readings open: the lease is named differently in code, it is implicit in provider lifetime, or
   "new" means reacquiring the one the stopped activation already held. Each implies a different
   answer to CBI50's finding 4 about what a stop does to an existing lease. Disposition: **open**,
   shared with CBI50. Contract vocabulary that no reader can trace to a mechanism cannot be reviewed
   for completeness, which is the point of recording it.

4. **Restarting into a retired generation is not excluded.** C4 carries forward the prior active CM4
   runtime observation and deliberately does not invent a successor generation. Nothing states that
   the generation is still active when the restart lands — a provider can be stopped, the generation
   retired by CM4, and a policy-approved restart then reconstruct a member into it. Disposition: the
   activation is opaque and retains its own resolution, so the reconstruction is internally
   consistent; whether the generation should still exist is a CM4 question CBI52 has no standing to
   ask. Recorded as a caller obligation: a host that retires a generation owns terminating its
   restart lineages, and CBI53 gives it the place to do so.

5. **Single-flight is in-process, and the contract's own limits say so.** C6 gives one stopped
   activation at most one successful successor via a claim that concurrent calls observe. The
   deliberate limits then state that CBI52 is not a cross-process lock. A host process that dies
   mid-flight loses the claim entirely, and a successor process sees no evidence a launch was ever
   attempted. Disposition: closed by construction across the run — CBI53 makes the attempt durable
   and CBI54 fences it across processes. C6 is honest within one process and is not load-bearing
   beyond it.

6. **"Verified again before launch" does not say what verification costs.** C3 re-verifies the
   complete retained set on every attempt. For a large staged set under an eight-attempt policy this
   is repeated in full, and no incremental or cached path is offered. Disposition: re-verification is
   the property being bought — C3's claim is that no completed restart executes bytes failing the
   retained identity, and a cache would weaken exactly that. Cost is accepted deliberately; recorded
   so a later optimisation does not read the silence as permission.

## Result

The reviewed contract covers policy recheck, recipe ownership, complete artifact re-verification,
same-generation repair, lifecycle rollback, claim behaviour under repeat and concurrency, and
retained-content ownership on every refusal path.

Findings 1, 4, 5, and 6 are closed by the contract's boundary or by the slices that follow it.
Findings 2 and 3 are **open**: C5 has no failed-termination branch where its sibling CBI50 has one,
and the lease vocabulary cannot be traced to a mechanism.

Durable attempt history, host-process-loss recovery, cross-process coordination, CM4 generation
replacement, artifact selection, and exhausted-content cleanup remain explicit non-goals owned by
CBI53, CBI54, and the distribution and maintenance boundaries.
