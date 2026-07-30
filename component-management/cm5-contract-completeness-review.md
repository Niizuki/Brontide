# CM5 contract-completeness review

Date: 2026-07-30

Status: complete; every finding below is closed

Scope: absence review of the experimental
[CM5 authority-admission capability contract](./cm5-capability-contract.md), kept separate from
implementation conformance review.

## Review method

The review asked, for each C1-C10 capability, what question two independent implementations could
answer identically because the contract never required an answer. It then checked boundaries where
multiple defects coincide, where participant input could select a local identity, where policy
could accidentally bypass a structural ceiling, and where an output could omit an input-dependent
decision. The shared vector inventory was treated only as stimulus, never as the complete contract.

## Findings and dispositions

1. **Which evidence satisfies a relationship rule was silent.** Requiring merely “some evidence,”
   or poisoning a request with every item the participant supplied, would let both implementations
   agree on an unintended trust rule. Closed by making each local relationship rule name its exact
   required evidence. Additional presented evidence remains visible but neither substitutes for nor
   poisons the required set.
2. **Validity boundaries were silent.** Two implementations could disagree exactly at issuance or
   expiry. Closed by defining an inclusive validity start and exclusive expiry, evaluated against
   the injected instant.
3. **Evidence with several defects had no reporting order.** A revoked, unverified item from an
   untrusted issuer could yield three equally plausible single outcomes. Closed with the explicit
   subject, verification, issuer, revocation, not-yet-valid, expiry precedence in C2.
4. **A denying relationship rule could still carry an apparent local Actor assignment.** Closed by
   requiring an admitting rule to assign a local Actor reference and a denying rule to assign none.
5. **Actor and rule identity uniqueness was incomplete.** Duplicate evidence, relationship request,
   authority request, mapping, trusted-issuer, or cross-kind policy-rule identities could make an
   explanation ambiguous. Closed with effect-free structural rejection of each duplicate.
6. **Unreferenced evidence could disappear from the observation.** Closed by rejecting evidence
   that no relationship request presents; every accepted input item therefore receives a
   relationship-relative evidence decision.
7. **Participant and proposed-Actor identity could diverge.** Closed by rejecting a relationship
   request that does not name the admission request's participant. The participant still cannot
   supply the receiving-domain Actor reference.
8. **Unlimited authority might reach a permissive or mistaken policy rule.** Closed by evaluating
   the unlimited marker as a non-overridable structural ceiling after relationship admission but
   before authority policy. It produces no grant or policy-mistake finding because no local rule was
   applied.
9. **A policy mistake might be mistaken for failed evidence or an attacker-created grant.** Closed
   by recording a finding only when the local rule is actually applied, and by retaining policy,
   rule, request, disposition, and rationale. Evidence failure remains a prior independent stage.
10. **Overall outcome categories were under-specified.** Closed by defining admitted,
    partially-admitted, denied (including an empty request), and effect-free invalid-request from the
    admitted/denied decision set.
11. **Partial admission could hide dependency scope.** Closed by making an authority request refer
    to exactly one relationship request and blocking only that request's dependent authority when
    the relationship is denied.
12. **A grant could widen or lose its provenance.** Closed by requiring every grant to repeat the
    requested Capability, target, Operation, and scope exactly and to record the local policy and
    rule that minted it.

## Deliberately outside CM5

CM5 does not define cryptographic verification, attestation protocols, trust-root management,
production Capability representation, Genesis persistence, Delegation, liveness monitoring,
Terminus, effect withdrawal after an already-authorised Execution begins, cross-domain federation,
or integration with CM4's fake activation transaction. The evidence status is an explicit fake
verifier observation. CM6 owns serialized/process-boundary comparison of the two native fake
implementations; equality there will prove only agreement on this tested model.

No unresolved contract-silence finding remains at the CM5 boundary.
