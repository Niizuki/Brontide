# CBI30 process-boundary activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §13.6, §18.1, §24, and §33, Complete Draft, not ratified

CBI30 is the first bounded step into the real-distribution area named after CBI29. It does not define
the Distributed extension or a package protocol. It connects the existing CBI2 activation path to
the existing negotiated Portable Binding realization over a real operating-system process boundary.
Component Management remains the deterministic fake host; the provider process is replaceable
behind the neutral portable contract.

## C1 — activation crosses a real process boundary

The composition root prepares the CM2-selected `1..1` member, validates CM4, and performs portable
Interconnection with a provider running in another operating-system process. The member reaches
Ready and is released only after CM4 accepts the activation.

Property: every successful CBI30 vector has an Active CM4 outcome and one released portable member.

## C2 — either stack's provider is substitutable at the process seam

Each Reference and Minimal composition root activates once against the Reference provider executable
and once against the Minimal provider executable. The host consumes only the portable contract and
process conversation; it imports no runtime type or assembly from the answering stack.

Property: changing only the provider executable does not change the stable activation outcome.

## C3 — the negotiated realization remains observable

Successful activation records `negotiated-process` in the frozen Binding Plan and records the
provider identity supplied by the answering contract. A process boundary is evidence, not an
inference from endpoint text or process placement.

Property: every successful vector reports the negotiated-process realization and the selected
provider identity.

## C4 — process loss is an explicit pre-Release refusal

If the provider process is unavailable during Interconnection, activation returns
`portable-process-interrupted`. It does not leak a foreign exception, fabricate a generic semantic
refusal, reach CM4 Release, or leave a portable member serving.

Property: every interrupted vector has no Active runtime and no released member.

## C5 — retirement closes the process lifecycle

Retiring a released member closes its ordinary-interaction gate, sends portable withdrawal and
termination, and permits the provider process to exit. Cleanup is explicit lifecycle traffic rather
than an operating-system kill presented as success.

Property: every successful vector can retire cleanly, becomes Retired, and observes provider exit.

## Boundary

CBI30 proves process isolation and cross-stack substitution for one already-supported direct
activation. It does not acquire an artifact, authenticate another domain, transport a Capability
across a trust boundary, define cryptographic identity or attestation, promise delivery or retry,
activate a Provider Set, or claim production distribution. Those require later contracts and the
future Distributed and Identity work.
