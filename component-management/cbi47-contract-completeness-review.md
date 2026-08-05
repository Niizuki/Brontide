# CBI47 contract-completeness review

Date: 2026-08-05

Scope: absence review of one bounded provider-trust cadence, separate from C1-C8 conformance.

## Findings and dispositions

1. **A serving-set snapshot taken before polling would already be stale when revalidation starts.**
   The policy attempt may wait through bounded retries while providers enter or leave service.
   Disposition: CBI47 obtains a current policy first and asks the host for its serving-set snapshot
   only afterward. A non-current poll never reaches the source.

2. **CBI46 deliberately refuses an empty input, but an idle host is not a cadence failure.** An
   adapter that forwarded an empty snapshot would turn normal idleness into
   `serving-trust-sweep-invalid`. Disposition: the CBI47 binding treats the empty snapshot as a
   successful no-op and does not invoke CBI46. CBI46's direct-call precondition remains unchanged.

3. **Successful withdrawal is enforcement, not scheduler failure.** Stopping after a withdrawn
   provider would prevent later members and later policy generations from being checked.
   Disposition: both current and withdrawn sweeps permit the next cycle; invalid, incomplete, and
   cleanup-incomplete sweeps stop before another interval.

4. **A completed delay is not evidence that its following cycle ran.** Cancellation can arrive
   while the host delay is returning. Disposition: CBI47 checks cancellation after the wait and
   records the gap only when the next cycle is admitted. A delay that does not advance its injected
   instant is a visible host error rather than an implicit busy loop.

5. **Cancellation cannot undo prior trust effects.** CBI41 may already have checkpointed policy and
   advanced the recovery floor; CBI46 may already have retired providers and attempted cleanup.
   Disposition: cancellation has exact observation boundaries but no rollback claim. A canceled
   cycle is retained when the poll reports it; cancellation during a gap adds neither a gap nor a
   cycle.

6. **A polling failure does not itself decide offline service policy.** Automatically withdrawing
   all serving providers would confuse lack of fresh policy with evidence that their publisher is
   unauthorized; silently continuing would make a different availability choice. Disposition:
   CBI47 stops without sweeping. Whether and when another bounded run starts is a host decision.

7. **A bounded in-memory loop is not durable scheduling.** Process exit loses the current cycle
   position, and overlapping runs could compete with the existing one-writer policy boundary.
   Disposition: the host owns run serialization and subsequent invocation. CBI47 provides no daemon,
   lease, durable job, crash cursor, retry queue, or restart protocol.

8. **Cadence does not close the remaining production-security boundaries.** Endpoint and authority-
   key rotation, recovery-floor custody outside the checkpoint writer's reach, and production
   process isolation require different authorities and failure models. Disposition: keep them
   separate rather than making a timer appear to provide them.

## Result

CBI47 is complete for one deterministic, bounded in-process run. The material ordering decisions are
poll before snapshot and record a gap only when its following cycle is admitted. Durable
retry/resumption, offline policy, restart policy, endpoint and key rotation, privileged floor
custody, and production isolation remain separate work.
