# CBI3 authority-gated portable activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI3 connects CM5 receiving-domain authority admission to the CBI1-CBI2 singleton lifecycle. The
first slice accepts one explicit typed mapping between the CBI1-selected occurrence and the CM5
participant Actor, one `ComponentParticipant` relationship request, and one narrow authority
request. CM5 admission is evaluated before provider contact; only one admitted local relationship
and one exact local grant permit CBI2 to establish and release the portable member.

## C1 — occurrence and Actor correspondence is explicit

The caller supplies one mapping naming the selected CM occurrence and the CM5 participant Actor.
The occurrence must equal the CBI1 selection, and the Actor must equal the authority-admission
request participant.

Property: changing either side of the mapping can only produce a refusal before CM5 evaluation or
provider establishment.

## C2 — the first authority shape is exact and narrow

The CM5 request contains exactly one relationship, of kind `ComponentParticipant`, proposed by the
request participant, and exactly one non-unlimited authority request dependent on that
relationship. The CM4 request contains no caller-authored binding exercises; mapping CM5 grants to
post-Release CM4 exercise evidence remains future work.

Property: additional relationships, authority requests, unlimited requests, other relationship
kinds, or caller-authored CM4 binding authority produce no provider establishment.

## C3 — CM5 admission precedes provider contact

After structural validation, the native CM5 evaluator runs before CBI1 preparation or portable
Interconnection. Only an `Admitted` outcome containing exactly one established relationship and one
local grant continues to CBI2.

Property: every CM5 denial, partial admission, or invalid request leaves no portable member,
Binding Plan, CM4 activation, or provider contact.

## C4 — the admitted grant remains exact and attributable

The admitted relationship and grant must name the submitted relationship and authority request.
Their Capability, target Actor, Operation, scope, policy, and rule remain the native CM5
observation; CBI3 neither widens nor reconstructs them.

Property: every successful CBI3 result contains exactly the one CM5 grant admitted for the one
submitted non-unlimited authority request.

## C5 — authority never crosses the portable trust boundary

CM5 admission is a receiving-domain decision controlling whether the composition may continue. No
CM5 Actor reference, Capability grant, evidence, policy, or decision is inserted into the portable
contract, Binding Plan, constraint value, or operation payload. PB7's no-Capability-transfer rule
remains unchanged.

Property: changing CM5 admission can change whether activation proceeds, but cannot change any
portable contract or Binding Plan fact.

## C6 — lifecycle failure cannot be converted into authority-backed activity

An admitted CM5 observation permits CBI2 to attempt activation; it does not guarantee portable
negotiation, Ready, CM4 Active, or Release. Any CBI2 failure remains visible, and CBI3 is successful
only when both CM5 is admitted and CBI2 returns a released Active member.

Property: every unsuccessful lifecycle leaves CBI3 inactive even when CM5 admitted the request.

## C7 — both composition roots implement independently

Reference Studio and Minimal Host own separate coordinators over their native CM5 and CBI2 types.
Neither experimental Component Management nor Portable Binding project references the other, and
neither stack references the other stack.

Property: deleting either coordinator leaves native CM5, CBI1-CBI2, and Portable Binding behavior
unchanged.

## C8 — evidence remains bounded

CBI3 proves only one receiving-domain Component-participant relationship and one exact narrow grant
gating one singleton, protocol-free activation. CBI5 separately covers exact grant revalidation
after activation. Multiple participants or grants, CM4 binding-exercise projection, cross-vocabulary Operation-to-invocation
mapping, multi-member release barriers, Relational Initialisation, replacement, child Ports,
serialized comparison, mediation, and wider Provider Sets remain future integration work.

Property: every CBI3 status statement preserves this boundary.
