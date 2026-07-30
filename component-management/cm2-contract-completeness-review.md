# CM2 contract-completeness review

Date: 2026-07-30

Review type: phase-boundary absence audit, separate from conformance and independent attestation

Scope: the CM2 C1-C10 capability contract, neutral vector inventory, Reference and Minimal public
surfaces, and both native test suites

Result: complete; every finding below is corrected and no unresolved CM2 contract silence remains

This review asks what the contract did not say. It does not claim Architecture 0.8 conformance,
cross-stack interoperability, production resolution policy, security, preparation, activation, or
independent review.

## C1 — immutable effect-free input

Finding: “snapshot” did not name when ownership detached, and a C# record graph could otherwise
retain caller-owned nested collections.

Disposition: the contract now fixes the snapshot point at `Resolve` invocation. Reference copies
every nested collection into resolver-owned arrays and read-only output collections; Minimal uses
persistent lists and returns new values. The merge-readiness audit also made every collection nested
inside a returned Reference Port envelope read-only rather than merely resolver-owned. Every outcome
exposes the all-false CM2 effect observation.

## C2 — recursive closure

Finding: CM2 said cycles were rejected while CM3 said finite compatible cycles were accepted,
without defining the phase boundary.

Disposition: CM2 detects every dependency or Composition Parameter cycle and returns
`cycle-requires-cm3`; it does not classify a cycle as compatible. CM3 owns strongly connected group
analysis. Contradictory definition identities, repeated requirement identities, and repeated
candidate observations from one source fail before traversal can choose between them.

## C3 — occupied stability

Finding: an occupied binding named an occurrence, but the first resolver draft could retain it
without proving that the occurrence inventory contained the same definition/occurrence pair.

Disposition: both implementations validate that pair before retention. A missing or mismatched
retained occurrence is `contradictory-identity`, never a fabricated active occupant.

## C4 — ranking and alternatives

Finding: deterministic selection collapsed mirrored source observations before the Proposed Stack
recorded alternatives, making source provenance disappear even though mirrors correctly could not
fill two positions.

Disposition: selection deduplicates by definition, while alternatives retain every source,
publisher, package, rank, admissibility decision, and exclusion reason. Candidate exclusion and
selection decisions are explicit. The merge-readiness audit additionally found that deduplicating
before policy filtering let a rejected earlier source hide an admissible mirror. Both resolvers now
record every rejected source and choose the best admissible observation per definition. Optional
capacity fills only through an admissible preselection.

## C5 — occurrence sharing

Finding: a candidate-level topology node was insufficient when one definition correctly produced
several non-shared occurrences.

Disposition: each proposed attachment occurrence receives a deterministic local node derived from
the candidate’s attributable attachment observation and the occurrence identity. Shared roles reuse
one occurrence and node; separate occurrences must have distinct nodes. Retained occurrences keep
their existing identity.

## C6 — exposure and Mediation

Finding: recording only the Mediation kind could hide whether the relationship was Host-erased or
owned mutable policy requiring a dedicated Component.

Disposition: the resolved record preserves kind, realization, dedicated Component identity, every
policy-bearing flag, all backing members, and every direct/non-direct Binding Plan. Any policy-bearing
static realization fails with `mediation-requires-component`.

## C7 — Regions and Ports

Finding: Port validation occurred, but the initial output omitted the containing Region, Port, and
the envelope that justified admission.

Disposition: Provider Sets now retain their Region and Port, while Proposed Stack and generation
records retain the ordered Port envelopes including lifecycle, contracts, cardinality, imports,
exports, authority, topology, failure, rollback, and widening policy. The merge-readiness audit
found that requirements could not yet express import, export, failure-policy, or rollback-boundary
demands. Those fields are now explicit in both native request models and are checked alongside every
other envelope dimension. Duplicate Port envelopes and duplicate retained occurrence proofs return
`contradictory-identity` rather than throwing or choosing one by enumeration.

## C8 — topology policy

Finding: accepted, refined, and rejected relations were attributable, but node uniqueness was not a
generation-wide invariant.

Disposition: both resolvers reject one occurrence mapped to several nodes or several attachment
occurrences mapped to one node. Relation refinement remains explicit and no topology decision
changes provider or authority observations.

## C9 — Activation Parameters

Finding: extra environment values were ignored without remaining inspectable, so a future
implementation could accidentally consume one structurally without an observation.

Disposition: unused Activation Parameter values are ordered Proposed Stack observations. Required
slot binding still runs only after structural closure and records environment or default provenance.

## C10 — complete explanation

Finding: the initial generation exposed member authority but omitted authority requested by included
root or structural definitions.

Disposition: Proposed Stack and generation records now include ordered requested-authority
observations for every included definition. The contract’s determinism property compares complete
semantic observations rather than claiming a canonical wire encoding that CM2 does not define.

## Residual boundary

CM2 does not decide that a dependency cycle is acceptable, compute strongly connected activation
groups, prepare artifacts, establish endpoints or Actors, grant authority, run lifecycle stages,
release interaction, cut over an active generation, or roll back. Those remain CM3-CM5 work.
