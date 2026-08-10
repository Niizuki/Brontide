# BR-07-PERSISTENT-INFORMATION-001 capability contract

Date: 2026-08-10

Status: experimental Architecture 0.7 Complete Draft implementation contract

Designed for: Brontide Architecture 0.7 sections 12, 18.2, and 21.1

## Boundary

This contract defines the R4/M4 evidence slice. It is limited to an Opaque Corpus, Dataset records,
Corpus-declared Store roles, logical Store endpoints, and a Router that presents one stable logical
endpoint over declared backing Stores. It does not define a database, transactions, Dataset
migration, Mirror or Backup relationships, a general storage vocabulary, or Brontide Base
conformance.

Reference and Minimal implement the behavior independently in experimental projects. Their only
common authority is this contract and the Architecture 0.7 text.

## Capabilities

### C1 — a Corpus makes identity and concurrency explicit

An Opaque Corpus has a distinct identity and version, one or more distinct Store roles, and an
explicit concurrent-access declaration. Construction refuses an absent declaration, no Store roles,
duplicate roles, no identity-bearing role, and a concurrency mode this experiment cannot enforce.

Property: every accepted Corpus has at least one identity-bearing role and exactly one supported,
explicit concurrency declaration.

### C2 — a Dataset is attributable authorised issuance

Dataset creation is performed as the effect of an Operation authorised by the existing stack-native
Capability evaluator. The Dataset records the already existing Actor that issued it and the
authorising Operation; the authority outcome records the execution. It is not created through a second Genesis path. A wrong Actor, Capability,
target, or Operation is denied before registry or Store effects.

Property: every Dataset in a registry names an issuer and Operation from a successful authority
evaluation, and every denied evaluation leaves the registry and all Stores unchanged.

### C3 — Dataset identity is independent of Store content

A Dataset carries a dedicated Dataset identity and binds every required Corpus role to exactly one
logical Store endpoint. Clearing, losing, or replacing Store content does not change the Dataset
identity. The role declaration, not the Store, says whether the role is identity-bearing and what
absence means.

Property: no Store identifier or content value is used as a Dataset identifier.

### C4 — operations preserve role and concurrency boundaries

Read and append address a Dataset and one Corpus-declared role. Unknown Datasets, undeclared roles,
missing role bindings, and a requested concurrency mode different from the Corpus declaration fail
visibly before a Store effect. The slice supports only the `single-writer` declaration, with writer
admission enforced by the existing authority boundary; it does not add a lock or writer lease.

Property: every failed Dataset operation leaves every Store observation unchanged.

### C5 — a Router owns its endpoint guarantees

A Router declares the guarantees of the logical Store endpoint it presents. Construction refuses a
guarantee that any declared backing or fallback path cannot uphold. The endpoint reports only the
Router declaration; it never copies additional guarantees from the selected backing.

Property: changing the selected backing cannot change the Router endpoint identity or guarantee set.

### C6 — Router fallback is bounded and visible without leaking policy

Append and read use the selected available backing, then the declared fallback order. If none is
available the operation fails visibly. An authorised management description may expose the selected
backing only when the Router policy declares that topology non-confidential. Ordinary callers and
confidential policies receive no backing identity.

Property: an unauthorised or confidential description contains no backing Store identity.

### C7 — identities remain distinct

Corpus, Dataset, Store-role, Store, Router, Actor, Capability, Operation, and execution identities are
not interchangeable public types. Bare primitives occur only at construction, diagnostic, and
external-data seams.

Property: every persistent-information record uses the dedicated identity type for its identity
space.

### C8 — each implementation owns native evidence

Reference NUnit tests and Minimal NUnit tests independently cover C1-C7, including missing
concurrency, wrong authority facts, unsupported concurrency, identity after Store loss, backing
change and fallback, unsupported Router guarantees, Store-guarantee leakage, and confidential
topology redaction.

Property: each C item has a named test in both stacks and each stack passes without referencing the
other implementation.

## Observation categories

Stable failure codes used by this experiment are data observations, not exceptions or shared runtime
types: `corpus-invalid`, `concurrency-unsupported`, `dataset-invalid`, `dataset-not-found`,
`role-not-found`, `role-unavailable`, `concurrency-mismatch`, `router-invalid`,
`router-guarantee-unsupported`, and `store-unavailable`. Authority denials retain the existing
stack-native authority outcome and occur before these experimental operations run.

## Deliberate limits

- Only the single-writer Corpus declaration is accepted, and every operation must name it. Writer
  admission remains an authority decision. External coordination is represented so it can be
  refused rather than silently treated as single-writer.
- Stores are deterministic in-memory evidence endpoints; persistence means content survives across
  Dataset operation objects while the endpoint exists, not durable media or crash recovery.
- Dataset deletion, migration, splitting, transactions, and multi-role atomicity are not defined.
- Router selection is deterministic declared order, not discovery, health prediction, or automatic
  rebinding of a Composition binding.
- Capability values do not enter either experimental project. Authority is exercised at the existing
  stack-native execution boundary whose authorised handler invokes the persistent operation.
