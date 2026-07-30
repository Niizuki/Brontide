# CM3 contract-completeness review

Date: 2026-07-30

Review type: phase-boundary absence audit, separate from conformance and independent attestation

Scope: the CM3 C1-C9 capability contract, neutral vector inventory, Reference and Minimal public
surfaces, and both native test suites

Result: complete; every finding below is corrected and no unresolved CM3 contract silence remains

This review asks what the contract did not say. It does not claim Architecture 0.8 conformance,
cross-stack interoperability, runtime activation, production lifecycle safety, Release, rollback,
or independent review.

## C1 — effect boundary

Finding: CM3's plan language could be read as running Local Initialisation through Ready even though
CM4 owns preparation, establishment, Release, restart, and rollback.

Disposition: CM3 now consumes declarations and emits an effect-free stage plan only. Every stage
records the ordinary gate closed, the plan ends at Ready with Release pending, and every outcome
reports an all-false effect observation including lifecycle execution and Ready reporting.

## C2 — group identity and ordering

Finding: “detect a strongly connected group” did not say whether isolated members, singleton
self-cycles, duplicate identities, or condensation ordering were observable.

Disposition: every member belongs to exactly one maximal SCC; singleton self-edges are cyclic,
isolated singletons are not, duplicate member and edge identities are refused, group identity comes
from the first ordered occurrence, and the condensation graph is emitted dependency-first. Native
permutation properties compare the complete outcome.

## C3 — finite closure

Finding: an ordinary dependency cycle and recursive descriptor expansion were both represented as
edges, so an implementation could accept both merely because the graph was finite in memory.

Disposition: descriptor-expansion participation in an SCC is a distinct
`recursive-descriptor-expansion` refusal. Ordinary post-Release cycles remain valid and create no
member startup order.

## C4 — exact provision evidence

Finding: checking whether a target had any matching provision silently accepted duplicate matching
provisions, while failures carried only prose rather than typed source, target, contract, and
version evidence.

Disposition: every non-structural edge now requires exactly one matching target provision. Both
failure models carry typed source occurrence, target occurrence, contract, and requested version in
addition to the edge identity and reason.

## C5 — lifecycle protocol ownership

Finding: protocol identity uniqueness did not prevent two different protocol identities from
claiming one edge. A relational edge between two SCCs was referenced, so it was neither unreferenced
nor present in either group's lifecycle plan.

Disposition: lifecycle protocol identity and owning-edge identity are both unique. Relational
Initialisation is explicitly group-internal in CM3; a cross-group relational edge is refused as
undeclared lifecycle traffic instead of disappearing. Every accepted relational edge retains one
complete, bounded, correctly directed protocol.

## C6 — Ready reachability

Finding: a member-level “can reach Ready” boolean would have been non-falsifiable and could hide
which input or peer caused a wait.

Disposition: members declare typed required and available local inputs plus explicit Ready waits.
Missing inputs and unknown peers are attributable, and the complete Ready-wait graph is checked for
cycles independently of the ordinary dependency graph.

## C7 — gate observations

Finding: merely omitting Release from the output did not prove that an implementation had kept
ordinary interaction closed during every planned stage.

Disposition: every emitted stage carries `OrdinaryGateOpen = false`. An observed ordinary
pre-Release edge is a structured refusal, a non-lifecycle edge cannot borrow a lifecycle protocol,
and unreferenced protocols are refused.

## C8 — Region containment

Finding: a Port identity alone did not prove that a cross-Region edge was declared in the correct
direction or covered by both import and export declarations.

Disposition: each cross-Region edge has exactly one edge-owned declaration matching source Region,
target Region, and Port with both import and export present. Missing evidence is refused or, only
when the edge explicitly permits it and names a Port, returned as a wider-parent proposal.
Same-Region crossing declarations and contradictory crossings fail closed.

## C9 — explanation and immutability

Finding: complete explanation needed to include isolated members, all inter-group edges, and every
accepted Region crossing, while Reference's nested public collections needed a separate read-only
guarantee.

Disposition: member and edge decisions cover the entire graph, including isolated members and
condensation edges, and the plan retains the complete ordered Region-crossing declaration set.
Reference snapshots every nested list and returns read-only collections; Minimal uses persistent
lists. Failures and wider-parent proposals contain no partial plan.

## Residual boundary

CM3 does not prepare artifacts, materialise Components, establish Actors, endpoints, resources,
Binding Plans, or authority, invoke lifecycle Operations, accept a Ready report, open the ordinary
gate, Release a generation, replace an active generation, scope a restart, or attempt rollback.
Those effects and their failure behavior remain CM4 work.
