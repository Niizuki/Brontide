# Integration guide

## Rules for coding agents

- Keep this project experimental and dependent only on Reference Core.
- Run Dataset mutations only inside a successfully authorised `AuthorityDomain` Operation handler;
  never accept a caller-supplied `authorized` flag.
- Keep Dataset identity in `DatasetRecord.Id`; never derive it from Store identity or content.
- Treat Router guarantees as declarations validated across every backing and fallback path.
- Add a named C-item test and observe it fail before changing behavior.

Create an `OpaqueCorpus` with explicit `SingleWriter` access and an identity-bearing Store role.
Register Dataset creation through `AuthorityDomain`, then call `DatasetRegistry.Issue` only inside the
authorised handler with the initiating Actor and Operation. Reads and appends address the typed
Dataset and Store-role identities and repeat the Corpus's declared concurrency mode.

For Architecture 0.8 D5 issuance, declare `DatasetAuthorityConstraint.Declaration`, give the
provider a Dataset-space-constrained Capability for resource Operations, and call
`DatasetRegistry.IssueWithAuthority` from the authorized creating handler. The returned resource
Capability is an ordinary Delegation to the requester; an exceeded provider scope is refused before
the Dataset or Capability is added.

A `RouterEndpoint` exposes only its own declared guarantees. Call `Describe(true)` only behind
management authority; a confidential Router still redacts backing identity. The in-memory endpoint
proves semantic persistence only for its lifetime. Crash recovery, transactions, migration, and
deletion remain outside this component.
