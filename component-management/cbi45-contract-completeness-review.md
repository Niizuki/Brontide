# CBI45 contract-completeness review

Date: 2026-08-04

Scope: absence review of one serving publisher-trust revalidation, separate from conformance tests.
It asks what the C1-C6 contract does not say.

## Findings and dispositions

1. **The first API could retire the wrong member.** It accepted a distribution result and an
   independently supplied lifecycle result. Both could be valid while belonging to different
   provider conversations, so revoking provider A could retire member B and terminate A. No shared
   vector distinguished the pairing. Disposition: CBI45 now issues an opaque serving activation by
   performing lifecycle activation over the launched provider conversation itself; revalidation
   accepts only that value. This was found by the completeness pass, not conformance.

2. **A moved policy identity is not a lapsed decision.** Disposition: the
   `unrelated-revocation` vector requires both implementations to continue under a different policy
   identity. As in CBI44, the current decision is compared rather than the snapshot.

3. **Later evaluation needs evidence provenance, not a new caller claim.** CBI44 originally
   reported policy identities but discarded the verified evidence. Accepting evidence on CBI45
   would let the later decision evaluate a value the launch chain never verified. Disposition: the
   chain result retains its internally verified evidence and policy-authority identity; CBI45 takes
   neither from its caller. This adds fields to Minimal's public F# record and is recorded as a
   breaking construction change.

4. **Graceful retirement and fail-closed process termination are different outcomes.** A portable
   retirement can fail while the host still controls the concrete process. Disposition: a trust
   refusal always proceeds to concrete provider termination and lease release; `RetirementCode`
   separately reports whether graceful retirement and staged-set cleanup completed. Cancellation
   after trust has lapsed is treated as retirement failure rather than reopening service.

5. **Staged-set removal can fail.** The ordinary vectors cannot provoke the store's filesystem
   failure without a separate injected store seam. Disposition: successful cleanup is the property;
   a failed removal is appended to `RetirementCode` and the trust refusal remains CBI35's. No false
   claim of zero residue is made for a reported cleanup failure.

6. **A matching registry cannot lose its current policy.** The registry advances and never clears,
   and the opaque activation retains the authority identity used by the chain. Disposition: keep a
   fail-closed `publisher-trust-policy-unavailable` guard for a future clearing transition, but do
   not manufacture a vector for a state version 0.1 cannot reach.

7. **Concurrent policy advancement is outside the slice.** The call reads one current snapshot and
   takes one decision. CBI38 remains bounded to one process and one writer. Disposition: no claim
   about a policy write racing the decision; a later call observes the later snapshot.

8. **A provider can exit independently of trust.** An activation already observed as exited is
   unavailable and causes no policy or cleanup effect. A process can naturally exit after any
   observation, so continued means the trust decision permits service, not that the process is
   guaranteed to remain alive.

9. **There is no invocation policy.** Nothing schedules the call, fans it out across a serving set,
   retries cleanup, or reacts to a policy update. Disposition: explicit boundary. Those are host
   orchestration capabilities, not hidden work inside a deterministic revalidation call.

10. **One provider and one member remain the bound.** Group replacement, multi-member withdrawal,
    and restart policy already have different component-management semantics. Disposition: do not
    infer fan-out or atomic group behavior from this one-member slice.

## Result

The CBI45 contract is complete for one explicit current-policy decision over one serving provider
and its bound portable member. The important correction is finding 1: two individually valid inputs
did not prove they belonged together. The opaque activation makes that relationship constructional
rather than a caller assertion.

The next boundary is host-owned invocation policy and fan-out across a serving set. Endpoint and
authority-key rotation, privileged recovery-floor custody, production process isolation, and
restart policy remain separate work.
