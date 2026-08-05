# CBI46 contract-completeness review

Date: 2026-08-05

Scope: absence review of one host-owned serving trust sweep, separate from C1-C7 conformance.

## Findings and dispositions

1. **Per-member cleanup could delete a staged set still used by a continuing sibling.** Two
   occurrences may legitimately launch the same content identity under different publisher evidence.
   Calling CBI45 independently would let the first withdrawn member remove those shared bytes before
   the second member's continuing result was known. Disposition: the sweep suppresses CBI45's local
   staged-set removal, evaluates every member, then removes an identity only when no swept activation
   using it continues. The mixed shared-identity vector pins retention. Standalone CBI45 keeps its
   one-member cleanup behavior.

2. **Caller order is not host policy.** Enumeration order can depend on discovery or collection
   implementation. Disposition: the activation retains its typed `OccurrenceId`, duplicate
   occurrences are invalid, and ordinal occurrence order is part of C2 and the shared vectors.

3. **Partial preflight would allow effects before discovering an invalid sibling.** Disposition:
   count, uniqueness, and serving availability are checked over the complete input before registry or
   store use. Empty, oversized, duplicate, and unavailable inputs return no observations.

4. **A withdrawal or cleanup failure must not short-circuit later members.** Disposition: all
   admitted activations are evaluated sequentially and the aggregate is derived only afterward.
   Cleanup failure remains separate from trust; a post-preflight availability race uses
   `serving-trust-sweep-incomplete` rather than masquerading as either.

5. **Preflight cannot freeze external process state.** A provider can exit between validation and
   its CBI45 call. Disposition: the per-member unavailable result is retained and the aggregate is
   incomplete. A staged identity with an unavailable observation is retained because CBI46 cannot
   prove cleanup or continued use either way. This call does not claim transactionality over
   processes it does not own.

6. **Policy advancement during the sweep is not serialized here.** CBI38 has one process and one
   writer, but the registry does not expose a pinned-snapshot CBI45 operation. Disposition: each
   observation reports its own serving policy identity. Hosts requiring one snapshot must serialize
   the writer around the bounded sweep; CBI46 does not claim atomic policy observation.

7. **Cancellation is not a rollback signal.** The C# surface preserves CBI45's cancellation seam;
   after trust lapses, CBI45 treats cancellation during graceful retirement as cleanup failure and
   still terminates the provider. F# provider conversation operations do not expose the same token.
   Disposition: no atomic cancellation claim. A future scheduler must record its own invocation and
   cleanup observations rather than treating cancellation as zero effect.

8. **This is invocation policy, not invocation cadence.** There is still no timer, watcher, policy
   update subscription, retry queue, or restart decision. Disposition: keep those as explicit future
   host capabilities rather than hiding a background service inside this deterministic call.

## Result

The CBI46 boundary is complete for one caller-triggered bounded serving set. The material correction
is finding 1: composing correct one-member cleanup naively would corrupt a shared multi-member store
relationship. Sweep-owned group cleanup preserves the shared artifact while any sibling continues.

Scheduling, durable retry/resumption, restart policy, authority-key rotation, privileged floor
custody, and production process isolation remain separate work.
