# CBI49 contract-completeness review

Date: 2026-08-05

Scope: absence review of provider-trust offline and reconciliation policy, separate from C1-C8
conformance.

## Findings and dispositions

1. **The retained policy has no expiry or issuance time.** Treating it as fresh for an arbitrary
   duration would invent a property the signed snapshot does not carry. Disposition: offline grace
   is explicitly host-configured and measured only from the last cycle that established current
   policy. The result is an availability choice, not new trust evidence.

2. **Not every retryable CBI41 failure is endpoint unavailability.** A stale signed window can be a
   clock or endpoint fault, and a superseded cursor means registry state changed during the attempt.
   Disposition: only exhausted transport failure and timeout are grace-eligible. Authentication,
   freshness, concurrency, authority, integrity, cancellation, and floor-retention outcomes stop.

3. **Offline grace must not become an admission path.** Continuing existing service under the last
   verified state is materially different from launching a new provider while current policy cannot
   be established. Disposition: the result always denies provider start, and an empty serving set is
   idle rather than permission to repopulate it.

4. **Repeated outage evaluation could accidentally slide the window.** Measuring grace from each
   failed attempt would make an outage renewable forever. Disposition: every decision uses the
   caller's retained last-current instant; failure time is not a freshness fact and never moves the
   deadline.

5. **Expiry is a decision, not yet an effect.** CBI46 cannot withdraw every member merely because
   policy distribution is unavailable: it evaluates publisher evidence against the retained policy,
   which may still admit them. Disposition: CBI49 reports `offline-service-stop-required` without
   pretending a trust sweep performed it. Host termination integration remains separate work.

6. **The journal cannot verify a no-effect claim.** Policy publication, floor custody, withdrawal,
   termination, and content cleanup live in other components. Disposition: evidence is explicitly a
   host reconciliation observation. CBI49 checks that it names the exact interrupted attempt but does
   not claim secure provenance or infer the verdict from journal state.

7. **An observed effect is not automatically safe to ignore.** Abandonment is valid only when the
   host reports those effects accounted for in its recovered policy, serving-set, and cleanup view.
   Disposition: distinguish `effects-accounted-for` from `unknown`; unknown remains in-flight.

8. **Reconciliation can race another journal owner.** A snapshot can become stale before the
   transition. Disposition: CBI49 rechecks the journal snapshot immediately before resolving, and
   CBI48 accepts the transition only while in-flight. This remains one-owner coordination, not
   locking or fencing across processes.

9. **Offline continuation says nothing about restart.** A provider already withdrawn is not an
   existing serving provider, and recreating it would be a new effect under possibly unavailable
   policy. Disposition: starts are always denied. Provider restart policy remains a separate next
   slice.

10. **A bounded duration can still overflow the injected time representation.** The maximum grace
    is only 24 hours, but adding it near `DateTimeOffset.MaxValue` initially escaped as an exception
    in both roots. Disposition: reject any observation whose deadline is not representable and clamp
    retry by comparing the remaining interval before addition. C1 pins the failure as
    `offline-observation-invalid`.

## Result

CBI49 is complete as an effect-free host decision boundary over explicit time, last-current evidence,
poll outcome, and reconciliation observation. Its material distinctions are availability versus
trust failure, existing service versus provider start, and accounted effects versus unknown effects.
Host enforcement of an expired grace period and provider restart policy remain later boundaries.
