# Brontide

New to the project? [Brontide: The Idea](./Brontide-Introduction.md) is the readable
introduction; the documents below are the precise ones.

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

[`Brontide-Architecture-Status.json`](./Brontide-Architecture-Status.json) identifies the current
architecture source and latest ratified architecture. Implementation targets are stated locally in
the document or stack README that owns the work; a central registry does not choose them. The
additional hashes and paths in the registry are retained for existing verification tooling, not as a
second implementation roadmap.

## Implementation targets

- [Brontide Reference Stack](./Reference/README.md) is designed for Architecture 0.7. Its README
  states what is implemented and which projects deliberately experiment against Architecture 0.8.
- [Brontide Minimal Stack](./Minimal/README.md) is designed for Architecture 0.7 under the same rule.
- A focused experiment or implementation note may target a different architecture revision by
  stating `Designed for: Brontide Architecture <version>` in that document.

A target records the architecture against which work was devised. It is not, by itself, a complete
conformance or ratification claim. Code, tests, and concise known-limitations prose remain the useful
evidence of what actually works.

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

The first extension of the Architecture 0.8 evidence cycle, `Channel`, is extracted from those two
interchange proofs and recorded in
[Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md): the request/Outcome
representation, correlation, error propagation, and delivery semantics the invocation principle
needs and Base withholds. It fixes semantics rather than a wire format, keeps Capabilities from
crossing a trust boundary, and precedes the Portable Component Binding, which becomes its first
conforming realisation. It remains a recorded direction outside Base, not a ratified extension.

The provisional generational lifecycle, multiple-source acquisition model, trust separation,
scoped restart, and dependency-cycle policy are recorded in
[Component Management and Distribution Design Note 0.1](./Brontide-Design-Note-Component-Management-0.1.md).
The corresponding
[Component Management Implementation Plan 0.1](./Brontide-Component-Management-Implementation-Plan-0.1.md)
calls for independent, entirely fake managers in both stacks so these mechanisms can be tested
without implying a real online marketplace, production package manager, or Architecture 0.8
conformance claim.

The broader topology direction is recorded in
[Topology Environments and the Guardian Family Design Note 0.1](./Brontide-Design-Note-Topology-0.1.md). Ordinary
Environments remain overlapping, security-neutral topology identities and have no Gatekeeper requirement.
A Guardian is an Actor entrusted to protect or represent a participant, resource, or bounded
interaction. Gatekeeper is its preventative Protected-Environment-boundary specialisation. Sentinel
is its bounded observational specialisation: the primary third-party observer and reporter within a
purpose-specific Sentinel Watch. The Watch makes subjects, occurrence classes, sources, coverage,
lifecycle, evaluator, outputs, and gaps explicit while granting no implicit response authority.
Protected Environments are disjoint or nested within one Protection Plane and opaque except through
Gatekeepers; one with no active Gatekeeper has no declared external communication. Every Gatekeeper export declares
its fidelity — Direct, Deputised, Mediated, Adapted, or Synthetic — so reinterpretation never
masquerades as exposure. These terms remain outside Base and are not a ratified extension.

Exact boundary assumptions are recorded in
[`docs/public-boundaries.md`](./docs/public-boundaries.md), and the reproducible manual/generated
source-cost inventory is [`interchange/binding-measurements.json`](./interchange/binding-measurements.json).
The current correction finding/deletion-gate status is summarized in
[`docs/implementation-correction-status.md`](./docs/implementation-correction-status.md).
The retained conformance matrices and independent-review workflow remain available as detailed test
and correction evidence. They do not determine either stack's architecture target; the owning README
does that directly.
