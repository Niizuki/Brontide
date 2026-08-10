# Integration guide

## Rules for coding agents

- Keep this project outside Model and Kernel; do not add a dependency back from either foundation.
- Invoke Dataset mutations from an Operation handler reached through `World.step`, after Kernel has
  evaluated Actor, target, Operation, Capability, and Constraints.
- Preserve private identity unions internally and unwrap only for diagnostics or external data.
- Refuse concurrency modes the experiment cannot enforce and preserve Store bytes on every refusal.
- Add a named C-item NUnit test and observe it fail before changing behavior.

Create an `OpaqueCorpus`, bind its roles to `IStoreEndpoint` values, and issue the Dataset from the
authorised handler with the request's initiating Actor and Operation. Reads and appends require the
Dataset identity, role identity, and declared concurrency mode. A `RouterEndpoint` may implement the
logical Store endpoint; its guarantee set is its own declaration, and backing identity is visible
only to authorised management when the Router policy is non-confidential.

For Architecture 0.8 D5 issuance, register `DatasetAuthority.constraintDeclaration`, give the
provider a `spaceRequirement`, and use `DatasetRegistry.IssueWithAuthority`. It preflights the
complete provider chain, runs `World.stepDraft08`, then returns the Dataset, derived requester
Capability, and next immutable `World` as one successful coordinator result.

The endpoint is intentionally in-memory. Durable media, restart recovery, transactions, migration,
and deletion remain out of scope.
