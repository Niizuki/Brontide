# CBI5 authority revalidation and grant-withdrawal capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI5 revalidates the one exact local CM5 relationship and grant that permitted a successful CBI3
activation. The caller supplies a fresh CM5 request with an explicit evaluation instant and current
evidence. If the same receiving-domain relationship and grant are no longer admitted, the
composition retires the released PB7 member, closing its ordinary-interaction gate before peer
withdrawal and termination.

## C1 — only a successful CBI3 activation can be revalidated

The input contains the prior CBI3 result, including exactly one admitted relationship, one exact
grant, and one released Active CBI2 member. An incomplete, refused, or already-retired result is not
silently treated as an active grant.

Property: every unavailable active state produces no CM5 evaluation and no new lifecycle effect.

## C2 — revalidation identifies the same authority

The fresh request must retain the prior admission-request and policy identities, participant,
`ComponentParticipant` relationship request, local authority request identity, and exact
Capability, target Actor, Operation, and scope. Its evaluation time, evidence state, validity
interval, trusted issuers, and policy rules may change. A mismatched request cannot replace the
authority that originally gated activation.

Property: changing any authority identity or tuple field can never keep the existing member active.

## C3 — time and evidence remain explicit CM5 inputs

The native CM5 evaluator receives the fresh request unchanged. CBI5 uses no ambient clock and does
not reinterpret revocation, expiry, evidence, or policy. The complete current CM5 outcome remains
visible in the result.

Property: accepted evidence changed to revoked, or evaluated at its exclusive expiry instant,
produces no current relationship or grant.

## C4 — continuation requires the same exact local relationship and grant

The member remains released only when CM5 returns `Admitted` with exactly the same established
relationship and grant, including receiving-domain Actor mapping, policy, and admitting rules.
Similar, wider, substitute, partial, or newly mapped authority is not continuity.

Property: every continued result contains one current relationship and grant exactly equal to the
ones that permitted CBI3.

## C5 — lost authority closes the ordinary-interaction gate first

When revalidation is mismatched or no longer admits the exact relationship and grant, CBI5 retires
the member through PB7. Retirement closes the ordinary-interaction gate before sending withdrawal
or termination lifecycle traffic. A clean retirement returns the replacement record; that record
grants nothing.

Property: after every withdrawn result, an ordinary interaction cannot reach the provider even
when the peer cleanup subsequently fails.

## C6 — cleanup failure is visible and remains fail closed

Provider withdrawal or termination failure produces a structured retirement failure. It cannot
restore the previous local authority or reopen the retired member. Because the peer state is
unknown, no successful replacement record is fabricated.

Property: every cleanup failure leaves the member non-released and reports no replacement as
permitted.

## C7 — both composition roots implement independently

Reference Studio and Minimal Host implement revalidation separately over their native CM5 and PB7
types. Shared material is limited to this contract and the data-only scenario inventory.

Property: neither implementation references the other stack's assembly, serializer, runtime type,
or private machinery.

## C8 — evidence remains bounded

CBI5 proves revalidation and fail-closed withdrawal for one local relationship and grant on one
released singleton binding. It does not authorize a portable invocation, withdraw an already
running execution, preserve state across replacement, handle several participants or grants, or
provide production identity, policy, distribution, or security.

Property: every CBI5 status statement preserves these limits.
