# Brontide

New to the project? [Brontide: The Idea](./Brontide-Introduction.md) is the readable
introduction; the documents below are the precise ones.

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

[`Brontide-Architecture-Status.json`](./Brontide-Architecture-Status.json) is the single
authoritative registry for the current architecture source, latest ratified architecture, retained
implementation baseline, and both stacks' delivery plans, ledgers, and evidence matrices. Generic
documentation and agent guidance resolve architecture identity through that registry rather than
hard-coding a version. Consumers must read the registry for the actual status values; an
implementation baseline is evidence, not ratification.

Known implementation and evidence gaps are controlled separately by the
[temporary implementation correction plan](./Brontide-Temporary-Implementation-Correction-Plan-0.1.md).
That file is a request for corrective work, not evidence that the work is implemented, and remains
until its explicit deletion gate is satisfied.

The first programme of real cross-stack evidence remains
[Reference/Minimal Interchange Implementation Plan 0.1](./Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented: two-way Cooling component interchange and a
materially different, resource-scoped Catalog interchange both cross real process boundaries. The
Catalog proof adds nested/repeated values, two Operations in one provider session, explicit failure,
resource refusal, replay detection, strict message variants, version skew, and a 65,536-byte line
limit. They test Brontide substitutability without sharing private CLR types or treating either
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

The provisional generational lifecycle, multiple-source acquisition model, trust separation,
scoped restart, and dependency-cycle policy are recorded in
[Component Management and Distribution Design Note 0.1](./Brontide-Design-Note-Component-Management-0.1.md).
The corresponding
[Component Management Implementation Plan 0.1](./Brontide-Component-Management-Implementation-Plan-0.1.md)
calls for independent, entirely fake managers in both stacks so these mechanisms can be tested
without implying a real online marketplace, production package manager, or Architecture 0.8
conformance claim.

The broader topology direction is recorded in
[Topology Environments and Gates Design Note 0.1](./Brontide-Design-Note-Topology-0.1.md). Ordinary
Environments remain overlapping, security-neutral topology identities; Gates create their
relationship-specific virtual-Component projections. Protected Environments are disjoint or nested
within one Protection Plane and opaque except through Gates; one with no active Gate has no
declared external communication. Every Gate export declares its fidelity — Direct, Deputised,
Mediated, Adapted, or Synthetic — so reinterpretation never masquerades as exposure. These terms
remain outside Base and are not a ratified extension.

Exact boundary assumptions are recorded in
[`docs/public-boundaries.md`](./docs/public-boundaries.md), and the reproducible manual/generated
source-cost inventory is [`interchange/binding-measurements.json`](./interchange/binding-measurements.json).
The current correction finding/deletion-gate status is summarized in
[`docs/implementation-correction-status.md`](./docs/implementation-correction-status.md).
Independent verification is coordinated by the checked
[`conformance/reviews/review-request.json`](./conformance/reviews/review-request.json) and the
[`independent-review workflow`](./conformance/reviews/README.md). The repository gate validates any
review records that exist and automatically refuses deletion of the temporary plan unless both
stack attestations and the explicit closure authorization are complete. The request pins the
architecture status registry and considers retained older matrices only as implementation evidence,
never as a replacement design source. Human and isolated automated attestations have equal weight
under the checked independence policy.

Implementation-owned status and limitations are recorded in the
[Brontide Reference Stack milestone evidence](./Reference/docs/milestone-evidence.md) and
[Brontide Minimal Stack milestone evidence](./Minimal/docs/milestone-evidence.md).

Stable implementation-baseline requirement IDs live in
[`conformance/requirements.json`](./conformance/requirements.json). The checked per-stack matrices
are [`Reference/conformance/architecture-0.5.json`](./Reference/conformance/architecture-0.5.json)
and [`Minimal/conformance/architecture-0.5.json`](./Minimal/conformance/architecture-0.5.json).
These files remain deliberately version-pinned evidence. Requirement IDs and matrices for the
registry-selected current architecture remain separate and must not rewrite retained evidence.
The current Complete Draft inventory is
[`conformance/architecture-0.7-requirements.json`](./conformance/architecture-0.7-requirements.json),
with independent [Reference](./Reference/conformance/architecture-0.7.json) and
[Minimal](./Minimal/conformance/architecture-0.7.json) matrices. They currently record tested C1
evidence and planned C2-C5 work without implying ratification or changing the 0.5 baseline.
