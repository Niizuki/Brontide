# CBI55 contract-completeness review

Date: 2026-08-05

Status: complete

## Review question

What could an interrupted provider restart do that the C1-C8 contract does not force a successor
owner to account for before selecting CBI53 `Retry`?

## Findings and dispositions

1. **A process id alone is reusable.** C5 therefore requires the receipt's process start instant and
   expected executable name as well as its id, token, and staged identity. Both implementations
   compare the complete set before termination.
2. **A receipt can survive after its provider exits.** C4 makes the operating-system lease the
   decisive no-live-provider observation. Stale or absent receipt bytes cannot prevent a retry once
   that lease is proved free, and receipt absence alone never proves the lease free.
3. **The current owner could reconcile its own still-running attempt.** C7 requires the effect
   record's fence to be strictly earlier than the current live CBI54 fence. A same-fence vector is
   shared by both roots and was deliberately observed failing when equality was allowed.
4. **Launching before durable preparation recreates CBI53's indeterminate gap.** C2 fixes the order:
   effect record, CBI53 in-flight transition, then CBI52 launch. Provider environment facts are
   derived from that record and do not replace the content-addressed argument policy.
5. **A killed process is not yet proof of cleanup.** C5 requires a bounded post-termination lease
   probe before selecting retry. A process that does not terminate or a lease that stays busy leaves
   the journal in-flight.
6. **The provider can perform effects outside the observable lifetime.** CBI55 cannot account for
   subprocesses, remote calls, or effects deliberately detached from the provider lifetime. The
   contract therefore limits its claim to the cooperating provider process and portable connection;
   broader effect custody remains future work.

## Result

The reviewed contract covers exact attempt identity, durable ordering, provider-held lifetime,
bounded receipt integrity, process-id reuse, stale receipts, exact orphan cleanup, post-kill proof,
journal silence on uncertainty, successor fencing, and independent shared evidence. No additional
in-scope capability is missing.

Provider adoption, remote/distributed ownership, hostile local forgery, detached effects, endpoint
or authority-key rotation, privileged rollback custody, and production isolation remain explicit
non-goals.
