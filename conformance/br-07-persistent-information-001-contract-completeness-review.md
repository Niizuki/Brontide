# BR-07-PERSISTENT-INFORMATION-001 contract-completeness review

Date: 2026-08-10

Status: complete phase-boundary absence audit

This review asks what the R4/M4 capability contract could otherwise leave unsaid. It is separate
from checking whether either implementation conforms to the contract.

## Findings closed in the contract

- **“Identity-bearing” did not require any identity-bearing role.** C1 now refuses a Corpus with
  none, so the declaration cannot be present but meaningless.
- **Capability-governed creation could have meant accepting a caller's Boolean.** C2 instead places
  creation inside the existing authority evaluator's successful handler and requires the issuer and
  Operation in the Dataset record, while the authority result records the execution. It explicitly tests Actor, Capability, target, and Operation
  denial before effects.
- **Identity independence could have been asserted only by type shape.** C3 requires a Store-loss
  observation in which the Dataset identity survives.
- **Declared concurrency could have been ignored by operations.** C4 refuses a mode the experiment
  does not implement and refuses operation requests that disagree with the accepted declaration;
  it also states that writer admission remains at the existing authority boundary.
- **Router guarantees could have been checked only against the initial backing.** C5 quantifies over
  every declared backing and fallback path and requires stability after selection changes.
- **Router transparency and confidential policy pointed in opposite directions.** C6 makes topology
  a separate management observation and requires both management authority and a non-confidential
  policy before a backing identity is shown.
- **A generic string identifier would make the identity claims non-falsifiable.** C7 enumerates the
  distinct identity spaces that the public surface must preserve.
- **Shared tests could make two implementations agree by construction.** C8 requires native named
  tests in both stacks; the shared item is the behavioral contract, not executable semantics.

## Residual limits

The experiment does not establish durable-media persistence, crash consistency, fairness between
writers, multi-role atomicity, or distributed Router health. Those require named operations and
failure observations before they can become tests. The single-writer claim is deliberately smaller:
the accepted Corpus declaration and every operation agree on the mode while other modes fail closed;
this contract does not claim an internal writer lock.
