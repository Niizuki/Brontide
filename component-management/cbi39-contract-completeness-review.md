# CBI39 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI39 authenticated fresh policy-distribution contract, separate from
conformance tests.

## Findings and dispositions

1. **A transport peer could claim any endpoint name.** Disposition: prevented at the response layer.
   The host pins an exact SPKI digest and verifies an ECDSA P-256 signature; source attribution alone
   conveys no authority.
2. **A valid response could be replayed.** Disposition: prevented across attempts. Every request uses a
   fresh 256-bit challenge committed by the response signature.
3. **A response for another local state could be delivered.** Disposition: prevented. The signed
   sequence and policy identity must exactly match both the request and the still-current registry.
4. **A captured response could outlive its intended period.** Disposition: bounded. Signed issue and
   expiry seconds are checked against the host clock with 30 seconds of future skew and a maximum
   15-minute validity interval. Clock correctness itself is not established.
5. **Remote work could be unbounded.** Disposition: bounded at the host seam. There is one source call,
   no retry, a caller timeout of at most one minute, linked cancellation, one optional update, a 1 MiB
   text budget, and 4096 policy entries. A source that ignores cancellation may continue its own work,
   but the client stops awaiting it at the timeout.
6. **Endpoint authentication could replace policy authority.** Disposition: prevented. The complete
   delivered update is committed by the endpoint response, then CBI38 and CBI37 independently verify
   the policy authority signature and monotonic predecessor chain.
7. **A partially accepted response could advance memory only.** Disposition: prevented. All response
   checks finish first, and the single update uses CBI38 publication-before-advancement semantics.
8. **Concurrent synchronization could apply against a changed cursor.** Disposition: detected. The
   cursor is rechecked after response validation; any later race is refused by CBI37 sequence and
   predecessor enforcement inside the durable apply.
9. **A valid endpoint could withhold a newer update.** Disposition: bounded but not eliminated. A
   signed current response is authoritative only for its short lifetime; no transparency log,
   quorum, or global latest-sequence oracle is claimed.
10. **Concrete network and operational delivery are absent.** Disposition: explicit. HTTP framing,
    bounded byte streaming, TLS policy, DNS, proxy behavior, retries/backoff, scheduling, endpoint
    discovery, key rotation, and availability remain outside CBI39.

## Result

The CBI39 contract is complete for a single authenticated, challenge-bound, fresh, bounded delivery
attempt into durable policy state. The next boundary should define a portable wire codec and concrete
bounded network transport adapter before production remote distribution is claimed; scheduling,
retry/backoff, and endpoint rotation remain later operational/security slices.
