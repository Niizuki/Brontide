# Future work

This directory is the authoritative entry point for planned, draft, proposed, work-in-progress, or
otherwise unimplemented work. A document belongs here even when it is the “current architecture” if
the implementations have not delivered it.

## Priority 1 — Portable Component Binding

[Portable Component Binding Implementation Plan 0.1](./binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
is the next implementation goal. It turns retained Cooling and Catalog experiments into a reusable
Binding Plan and Channel realization. Its first stage, PB0, inventories the existing Cooling and
Catalog behaviour, maps it to the C1-C10 capability contract and the Channel 0.1 vectors, and
creates the data-only neutral contract under [`binding/portable/`](../../binding/portable/README.md).

**PB0 through PB7 are complete.** The PB0 scaffold, C1-C10 baseline inventory, representation
choice, and both resolved owner decisions are recorded there; PB1 authored the neutral contract
itself, as eight data-only schemas, 63 vectors covering C1-C10 and all 24 Channel 0.1 vectors, and
deterministic golden CBOR encodings — the later additions are Decision 5's eight Catalog vectors and
PB7's ninth schema with its eleven, taking the total to 82; PB2 implemented that contract natively in the Reference
stack under [`Brontide.Reference.Experimental.Binding/Portable/`](../../Reference/src/Brontide.Reference.Experimental.Binding/Portable/);
and PB3 implemented it independently in the Minimal stack under
[`Brontide.Minimal.Binding/Portable/`](../../Minimal/src/Brontide.Minimal.Binding/Portable/), using
Minimal-owned algebraic types and explicit results rather than a translation of the Reference
surface. Cooling and Catalog are fixtures over the reusable layer rather than definitions of it.
[`build/verify-portable-binding.ps1`](../../build/verify-portable-binding.ps1) validates the neutral
layer without loading either stack and then runs both stacks' evidence against it.

**PB4 measured the two realizations against each other** across every portable result class a host
can reach, rather than only the success and denial PB2 and PB3 compared. Doing so found four
divergences — the same four in each stack independently — and closed them: an endpoint-decided
refusal reported two different failure domains, and an authority-bearing request body was rejected
under two different categories depending on which rule happened to fire first. Every Channel 0.1
vector now has executed evidence in each stack, derived from the neutral declarations rather than
restated, and the parity profiles are reproduced against a provider in its own process. The three
migration obligations PB1 recorded — deterministic map key ordering, schema-guided values, and
frameless denial — were discharged on both sides in PB2 and PB3; they are described in
[`binding/portable/README.md`](../../binding/portable/README.md).

**PB5 paired the implementations.** All six combinations of the cross-stack matrix pass: each stack's
host against the other's provider, each host against an
[implementation-neutral provider](../../binding/neutral-provider/README.md) that imports no Brontide
assembly, and both fixed direct calls. Pairing them found two things four phases of independent work
had not — that Catalog was never a shared contract, and that the neutral fixture declaration was not
encodable as published, because it carries documentation fields the contract rejects. Both are fixed
in the neutral data, and no neutral vector is deferred in either stack any more.

**PB6 hardened both stacks.** Decoders are property-tested inside deterministic bounds, failure paths
are proved to leak no provider effect, value, runtime type, resource, or false success, transport
failures classify totally into declared process categories, and the C6 and C8 refusals are now
decided by an endpoint across a real seam rather than by a codec or a lifecycle object called
directly.

Testing properties rather than cases found three defects, each present identically in both stacks.
Resource observations claimed an acceptance and an integrity check that never happened, in fields the
parity profile compares. The transport let foreign exceptions escape the binding. And two declared
process categories had no path that could produce them.

That they appeared in *both* stacks is the finding worth carrying forward. The programme's central
safeguard is independent implementation, and independent implementation is exactly what cannot catch
these: two stacks written from one contract by one reader diverge where the contract is ambiguous —
which is what PB4 and PB5 found — and agree wherever it is silent, which is where PB6's defects
lived. The plan's PB6 section records all three, the ordering rule that a malformed frame is refused
before its direction is weighed, and why `peer-unavailable` and premature resource reuse stay
unreachable in version 0.1 rather than being given manufactured paths.

**All eight owner decisions raised by PB4, PB5, and PB6 were recorded on 2026-07-28**, with their
option sets and rationale retained in
[`binding/portable/open-decisions.md`](../../binding/portable/open-decisions.md) and dated rulings in
the plan's [Resolved questions](./binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md#resolved-questions).
Four confirmed the provisional choice unchanged; four created work, all of it now done.

Decision 10 is the one to read first, because it is about the programme rather than the binding.
Given that every PB6 defect was present identically in both stacks, it asks what supplements
independent implementation, and answers with two standing practices: **every capability states at
least one property holding over all its vectors**, and **each phase boundary gets a
contract-completeness review** asking what the contract does *not* say. Both are now ground rules in
[`AGENTS.md`](../../AGENTS.md). The reasoning is that two implementations written from one contract
by one reader diverge where it is *ambiguous* and agree where it is *silent*, so independence detects
ambiguity and is structurally blind to silence.

Decision 5 also gave the Catalog fixture the vectors it had never had — PB-64 through PB-71, executed
in both stacks — closing the case where the neutral layer declared what Catalog is without stating
what it must do. Doing so found that the Catalog *handlers* had drifted apart across all three
implementations on partial-match and count semantics, which the fixture contract never declared.
PB-70 and PB-71 settle both, with the rules derived from the declared Shapes rather than adopted from
whichever implementation was read first, and Minimal brought to them.

One question remains open, and it is not one of the eight: whether to ratify the provisional Channel
Shape and category names or publish an explicitly migrated revision.

**PB7 added the Composition handoff**: the narrow seam by which a resolved Component requirement and
an offered provision produce a Binding Plan during activation preflight. It consumes a resolution and
never produces one, so four of its eleven vectors (PB-72 through PB-82) are refusals — a Provider
Set, a mediated exposure, a provider the resolution did not select, and a Component the three inputs
disagree about — because approximating any of them would make the Component Management programme's
decisions here, invisibly. A controlled experimental composition in each stack establishes and
releases bindings across both realizations, with the ordinary-interaction gate closed until every
required member is ready. The seam was declared in
[`binding/portable/schemas/composition-handoff.json`](../../binding/portable/schemas/composition-handoff.json)
before either stack implemented it, which is Decision 10's completeness practice applied at the front
of a phase rather than at its boundary.

Pointing a new consumer at the layer found what six phases of using it had not: **negotiation never
compares provider identity**, so the Binding Plan's provider fact reports who the host asked for
rather than who answered. Both stacks do this identically, and every fixture derives from one
declaration, so the two have always agreed. The handoff checks it provisionally; the contract
question is **Decision 11**, the one decision now open, and another instance of the gap Decision 10
names.

**PB8 is partly complete.** Its evidence and documentation work is done: the contract matrix now
carries an executed-evidence table naming which realizations have run each capability; the Channel
ledger records CH-R11 as executed by a conforming realisation rather than awaiting stack harnesses;
the public boundary document gains a portable-seam section; both stacks' changelogs record the added
experimental surface; and the source-cost inventory has been re-measured, separated into retained and
portable layers, and extended with the representation, framing, allocation, copy, and payload-bound
facts for both realizations, each stating how it is known. The complete repository gate is green.

Two PB8 steps remain, and neither is the implementer's to close: **fresh independent reviews** of
Reference, Minimal, and the neutral contract, which require a reviewer identity distinct from every
implementation actor in a fresh context; and **question closure**, which requires owner rulings on the
Decision 11 rather than an implementer writing a provisional choice down as a decision.

The former Priority 0 documentation relocation is complete; its archived plan is the
[Pinned Documentation Relocation Plan 0.1](../archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md).
No documentation prerequisite now precedes planned implementation work.

## Priority 2 — Component Management

[Component Management Implementation Plan 0.1](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md)
is the next implementable programme while Portable Binding awaits reviewer and owner actions. CM0
and CM1 are complete independently in both stacks. CM1 adds standardised contract/version
discovery across zero or more fake sources, deterministic attributable candidates, immutable staged
acquisition, contested evidence with attributable fake-policy decisions, source disappearance, four
structured fail-closed acquisition categories, and an explicit zero-effect boundary. Its C1-C7
behaviour and phase-wide properties live in the data-only
[`CM1 capability contract`](../../component-management/cm1-capability-contract.md).

CM2 — recursive generational resolution — is the next implementation phase. CM1 does not select a
candidate, construct a Proposed Stack, resolve a generation, prepare or activate a Component,
establish an Actor, or grant authority.

## Other planned areas

| Area | Planning source | Current implementation state |
| --- | --- | --- |
| Architecture 0.8 | [`Brontide-Architecture-0.8.md`](./architecture/Brontide-Architecture-0.8.md) | Complete draft; implementation evidence pending; not ratified. |
| Channel | [`Channel Design Note`](./channel/Brontide-Design-Note-Channel-0.1.md), [`Draft Channel Contract`](./channel/Brontide-Draft-Channel-Contract-0.1.md), and [requirements ledger](./channel/architecture-0.8-channel-requirements-and-risk-ledger.md) | Cooling/Catalog evidence exists; reusable Channel realization remains planned. |
| Component Management | [design note](./component-management/Brontide-Design-Note-Component-Management-0.1.md) and [`implementation plan`](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md) | CM0–CM1 are implemented independently in both stacks; CM2–CM6 remain planned. |
| Composition | [`Composition Design Note`](./composition/Brontide-Design-Note-Composition-0.1.md) and [Composition Without a Kernel](./architecture/Brontide-Architecture-Composition-Without-a-Kernel.md) | Experimental composition evidence exists; the proposed architecture is not ratified. |
| Enrichment | [`Enrichment Design Note`](./enrichment/Brontide-Design-Note-Enrichment-0.1.md) | Targeted experimental evidence exists; the wider design remains work in progress. |
| Persistent Information | [`Persistent Information Design Note`](./persistent-information/Brontide-Design-Note-Persistent-Information-0.1.md) | Design direction only. |
| Topology and Guardians | [`Topology Design Note`](./topology/Brontide-Design-Note-Topology-0.1.md) | Recorded design direction; not ratified. |
| Reference 0.3 plan | [`Reference implementation plan`](../../Reference/docs/future/Brontide-Reference-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |
| Minimal 0.3 plan | [`Minimal implementation plan`](../../Minimal/docs/future/Brontide-Minimal-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |

Planned documents must state what is already implemented separately from what remains. When a plan
is completed, move it to `docs/archive/<area>/` and move lasting operational guidance or evidence to
`docs/current/` or the owning implementation.
