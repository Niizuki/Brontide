# CM5 authority-admission capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §24, Complete Draft, not ratified

CM5 consumes explicit Actor-relationship and Capability requests, attributable evidence, an
evaluation instant, and one fake receiving-domain policy. It deterministically models local Actor
reference creation and narrow local Capability grants. The Reference and Minimal stacks implement
this contract independently.

CM5 is not an identity protocol, cryptographic verifier, policy recommendation engine, or production
Capability implementation. Its evidence observations are already-verifier outputs. The harness
proves the separation between a participant's claims, evidence evaluation, local policy, Actor
relationship establishment, and authority establishment.

## C1 — requests and claims have no effects

The participant may propose an Actor identity and request a relationship or Capability, but the
request contains no locally effective Actor reference or grant. A local Actor reference can be
assigned only by an admitting relationship-policy rule, and a local grant can be minted only by an
admitting authority-policy rule after that relationship is established.

Property: every request lacking an admitting local decision produces no Actor reference or grant,
regardless of the names or purported authority supplied by the participant.

## C2 — evidence is attributable and independently evaluated

Every evidence item has its own identity, issuer, subject, verification result, validity interval
(inclusive start, exclusive expiry), and revocation state. Evaluation uses the supplied instant and the receiving policy's trusted
issuers. Evidence is recorded as accepted, unverified, untrusted, not-yet-valid, expired, revoked,
or subject-mismatched before any relationship or authority policy is applied.
When several defects apply, the single decision uses this deterministic precedence:
subject-mismatched, unverified, untrusted issuer, revoked, not-yet-valid, expired, accepted.

Property: no revoked, expired, not-yet-valid, unverified, untrusted, or subject-mismatched evidence
can satisfy an admission rule, even when that rule would otherwise allow the request.

## C3 — Actor relationships are local mappings

A relationship request names the participant's proposed Actor and relationship kind plus the exact
evidence it presents. A matching local rule names the exact evidence it requires and may deny the
request or assign a receiving-domain Actor reference.
The participant cannot propose or select that local reference. Duplicate request or evidence
identities, missing evidence references, and mismatched participants are invalid input.

Property: every established relationship names the exact local policy and rule that assigned its
local Actor reference; additional evidence not required by that rule cannot substitute for or poison
the required evidence.

## C4 — authority is admitted only after relationship establishment

An authority request refers to one relationship request and names one Capability, target Actor,
Operation, and scope. It is evaluated only after that exact relationship is established. Actor
admission, compatibility, evidence acceptance, and activation are not Capability grants.

Property: every local grant traces to one established relationship and one admitting local
authority decision; no denied relationship can have a grant.

## C5 — grants are exact and narrow

An authority rule matches the complete requested tuple: relationship kind, Capability, target,
Operation, and scope. A grant repeats exactly that tuple and cannot widen, union, substitute, or
infer authority. A request marked as unlimited is structurally denied before local policy, even if a
matching rule would allow its nominal tuple.

Property: every grant is an exact subset of one non-unlimited request, and no output contains an
unrequested Capability, target, Operation, or scope.

## C6 — unknowns and absent policy fail closed

Unknown relationship kinds, Capabilities, targets, Operations, scopes, issuers, evidence references,
or missing policy mappings are visible denials or invalid-input outcomes before a grant. Structural
similarity, source presence, acquisition, activation, and possession never substitute for a mapping.

Property: every unresolved or unknown authority boundary produces zero grants for that request.

## C7 — local policy mistakes remain attributable local decisions

A fixture may mark a policy rule as a known mistake. The fake Host still applies that local rule so
the architecture's trusted-computing-base limit is observable, but records a policy-mistake finding
with the policy, rule, request, decision, and rationale. Structural input validation, evidence
validity, and the unlimited-authority ceiling remain non-overridable.

Property: every applied rule marked as a mistake produces exactly one attributable finding, and no
participant claim is relabelled as the cause of that local decision.

## C8 — withdrawal is explicit and time is injected

Re-evaluating the same semantic request against revoked evidence or a later supplied instant can
withdraw relationship admission and therefore every dependent grant. CM5 does not use an ambient
clock and does not silently preserve a prior grant.

Property: when required evidence changes from accepted to revoked or expired, the resulting
observation contains no dependent Actor relationship or Capability grant.

## C9 — partial admission preserves independent decisions

Several relationship and authority requests are evaluated independently in stable identity order.
One denial does not erase a separate accepted narrow grant, while a denied relationship blocks only
its dependent authority requests. The overall outcome distinguishes admitted, partially admitted,
denied, and invalid request.

Property: permuting semantically equivalent inputs does not change any evidence, relationship,
authority, grant, finding, or overall outcome observation.

`admitted` means every submitted relationship and authority request was admitted and at least one
was submitted. `partially-admitted` means the observation contains both admitted and denied
decisions. `denied` means it contains no admitted decision, including an empty request.
`invalid-request` is reserved for structurally contradictory input and is effect-free.

## C10 — the explanation is complete and immutable

The result snapshots the request identity, policy identity, evaluation instant, evidence decisions,
relationship decisions, authority decisions, established Actor references, local grants, policy
mistake findings, and a deterministic decision log. Inputs and outputs are copied at the boundary.

Property: every grant and denial has a recorded stage-local reason, and no output effect exists
without a corresponding evidence and policy trail.

## Structured outcomes

CM5 returns exactly one of:

- `admitted`;
- `partially-admitted`;
- `denied`;
- `invalid-request`.
