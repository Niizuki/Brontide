# CBI2 portable lifecycle orchestration capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI2 connects one CBI1-prepared portable member to the fake CM4 activation runtime. The first slice
supports one singleton, protocol-free CM3 activation group. It derives CM4 stage observations from
the portable member rather than accepting caller claims about Local Initialisation,
Interconnection, or Ready.

## C1 — the selected occurrence is exact

The CM3 plan must contain exactly one group and one member, whose occurrence is the CBI1-selected
occurrence. The group must carry no Relational Initialisation protocol.

Property: adding, removing, or replacing the member can only produce a refusal before provider
establishment.

## C2 — CM4 preflight precedes provider establishment

The coordinator derives hypothetical successful stage observations and evaluates the pure CM4
runtime first. If CM4 would refuse the request or fail Release/cutover, the portable provider is
not contacted and the member stays in Local Initialisation.

Property: every CM4 preflight refusal has no portable Binding Plan and no provider establishment.

## C3 — portable state is the stage witness

Local Initialisation is witnessed by the prepared member. Interconnection succeeds only when PB7
negotiates a Binding Plan and confirms the selected provider. Ready succeeds only when the
portable lifecycle reports Ready.

Property: the coordinator never reports successful Interconnection or Ready from caller-supplied
CM4 stage outcomes.

## C4 — portable refusal becomes CM4 establishment failure

A portable Interconnection refusal produces failed CM4 Interconnection and Ready observations and
the resulting CM4 `EstablishmentFailed` prefix. It never proceeds to CM4 Release or portable
Release.

Property: every portable establishment failure leaves the ordinary-interaction gate closed.

## C5 — release order is one-way

After actual Interconnection and Ready, the coordinator evaluates CM4 with the derived successful
observations. Only an `Active` CM4 outcome permits the portable member's Release transition.

Property: a released portable member always has an Active CM4 observation; CBI2 success is never
returned until portable Release succeeds.

## C6 — caller stage claims are non-authoritative

The coordinator replaces the request's entire `StageOutcomes` collection with observations derived
for the supported member. Other explicit CM4 inputs—scope, retained generation, preparation,
interactions, bindings, Release failure injection, rollback, and child declaration—remain
authoritative and are validated by CM4.

Property: changing caller-supplied stage outcomes alone cannot change a CBI2 result.

## C7 — both composition roots implement independently

Reference Studio and Minimal Host own separate coordinators over their native CM4 and PB7 types.
Neither underlying experimental component references the other, and neither stack references the
other stack.

Property: deleting either coordinator leaves CM4 and Portable Binding behavior unchanged.

## C8 — evidence remains bounded

CBI2 proves only singleton, protocol-free lifecycle alignment. Multi-member release barriers,
Relational Initialisation, replacement/retirement, child Ports, CM5 authority admission,
cross-process comparison, mediation, and wider Provider Sets remain future integration work.

Property: every CBI2 status statement preserves this boundary.
