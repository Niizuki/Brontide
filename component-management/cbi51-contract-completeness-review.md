# CBI51 contract-completeness review

Date: 2026-08-10

Status: complete

Backfilled: this phase boundary was originally recorded only as an inline section of the
[CBI51 capability contract](./cbi51-capability-contract.md). The review below was performed against
the merged slice at the later date and is the standalone absence audit the practice calls for.

## Review question

CBI51 turns retained trust into a restart decision by demanding a current-cycle proof. What can be
true of that proof, or of the attempt history beside it, that the C1-C8 contract does not force the
caller to account for?

## Findings and dispositions

1. **The attempt history is caller-supplied and nothing binds it to this activation.** C1 accepts
   "the prior attempt observation" as an input, and C7 validates it only for internal coherence —
   counts in range, instant present, instant not in the future. A caller that supplies another
   activation's history, or a stale copy of this one's, gets a well-formed decision computed from the
   wrong lineage, and C5's exhaustion rule silently protects nothing. Disposition: this is the
   silence CBI53 exists to close, by binding one journal to one occurrence and staged identity and
   deriving the history rather than accepting it. Recorded here because CBI51 alone is not safe to
   drive from unbound history, and its contract does not say so.

2. **A current-cycle proof is verified, not bounded.** C2 requires the supplied policy identity to
   equal the registry's current identity, which makes the proof exact at the moment it is compared.
   Nothing bounds how old the observation behind it may be, and the registry can rotate between the
   proof and the relaunch CBI52 performs on this decision. Disposition: exactness at comparison time
   is the whole claim, and CBI52/C1 re-evaluates CBI51 inside the enforcing call rather than trusting
   a decision carried from elsewhere. The window is narrowed by that recheck, not closed; CBI51 is a
   decision, and a decision cannot promise the world holds still after it.

3. **C3 is implemented as a denylist, and nothing in CBI51 makes that safe.** C3 reads as an
   enumeration of four causes, but both roots refuse `PublisherTrustWithdrawal` and
   `OperatorRetirement` and let everything else fall through to eligibility. C3's property — "no
   terminal cause produces restart readiness" — quantifies over the two terminal values rather than
   over the complement of the eligible ones, so it would not detect a fifth value reaching the
   eligible branch, and C8's shared vectors would not either, because every vector supplies a valid
   cause.

   What makes the denylist safe is entirely outside this slice, and differently in each root. Minimal's
   `ProviderRestartCause` is a closed union with a private constructor, so no fifth value exists.
   Reference's is a C# `enum`, where `(ProviderRestartCause)99` is representable — but it cannot reach
   `Evaluate`, because CBI67 guards every path that produces the attribution the policy reads:
   `ProviderStopAttribution` has an `internal` constructor and get-only properties, so no caller
   outside the assembly can build one or `with` one; `Record` throws `ArgumentOutOfRangeException` on
   any cause outside its allowlist; and the deserializer rejects a record whose cause fails
   `Enum.IsDefined`. `UnexpectedExit` is never persisted at all — the store synthesizes it, with a null
   instant, to represent the absence of a record, which is what an unexpected exit looks like.

   Disposition: closed — the fall-through is unreachable in both roots, verified rather than assumed.
   What the review records is that CBI51's safety here is **non-local**: its contract states a rule its
   code does not enforce, and the enforcement lives in a slice sixteen boundaries away. A reader
   checking C3 against `ProviderRestartPolicy` alone cannot confirm it. Inverting the branch to an
   allowlist would make the rule local, but it is deliberately not proposed here: the case is
   unreachable, so no test can name a trigger for it, and AGENTS.md is explicit that a test without a
   nameable trigger should be left as a comment or left out rather than freezing current behaviour
   into a contract. A defence-in-depth edit with no test that can fail is not obviously an improvement
   over a comment pointing at CBI67.

4. **Refusing a newly untrusted publisher looks the same as refusing one never trusted.** C4
   re-evaluates retained evidence against proven current policy and refuses with CBI35 attribution.
   For an operator, "this publisher was revoked while your provider was down" and "this evidence
   never satisfied policy" are different events with different responses, and the contract does not
   say the attribution separates them. Disposition: CBI35 attribution names the evidence and the
   policy decision, which is enough to distinguish the cases at the point of reading, and CBI51 adds
   no verdict of its own. Restart refusal is deliberately not an incident channel; operator-facing
   distinction belongs with attribution reporting, not this decision.

5. **Monotonicity is a per-call property and time is injected per call.** C5's property — changing
   only time moves waiting to ready and never back — holds within one evaluation. Across two calls
   the caller supplies the instant, and a caller whose clock moves backwards can observe ready and
   then waiting. C7 rejects a last-attempt instant in the future relative to the supplied now, which
   catches the inverted pair only when the history is carried forward. Disposition: injected time is
   the caller's fact and the contract does not claim a monotonic source. The property is stated over
   the evaluation, which is what it can be stated over; a host wanting the stronger guarantee needs
   the durable history of CBI53, where the committed instant is the one compared.

6. **Eight attempts at up to an hour is an unbounded-looking total.** C1 bounds attempts and delay
   independently, so the maximum lineage spans roughly eight hours with no stated total budget.
   Disposition: the two bounds are the whole policy and no wall-clock ceiling is claimed. Recorded
   because "bounded" in C1 means bounded in attempts, not in time, and a reader can take it for both.

## Result

The reviewed contract distinguishes retained from proven-current policy, stopped from still-serving,
availability from trust withdrawal, retention from authority, and eligibility from effect. It states
the first attempt, the delay boundary, exhaustion, malformed history, future time, and overflow.

Findings 2 through 6 are closed by the contract's boundary, by the type it is written over, or by
CBI67's guards. Finding 1 is a real silence and is closed by CBI53 rather than by CBI51; until a
caller drives this policy from a bound durable lineage, its exhaustion and delay rules are advisory.

No finding in this review requires a code change. Finding 3 is worth carrying forward as a reading
note rather than a defect: C3's rule is enforced entirely by CBI67, and both the contract and the
policy source are silent about that dependency.

Choosing a provider executable, rebuilding a lifecycle request, persisting attempt history,
coordinating concurrent owners, and crash recovery during launch remain explicit non-goals owned by
CBI52 through CBI54.
