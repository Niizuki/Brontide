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

**PB0 through PB3 are complete.** The PB0 scaffold, C1-C10 baseline inventory, representation
choice, and both resolved owner decisions are recorded there; PB1 authored the neutral contract
itself, as eight data-only schemas, 63 vectors covering C1-C10 and all 24 Channel 0.1 vectors, and
deterministic golden CBOR encodings; PB2 implemented that contract natively in the Reference
stack under [`Brontide.Reference.Experimental.Binding/Portable/`](../../Reference/src/Brontide.Reference.Experimental.Binding/Portable/);
and PB3 implemented it independently in the Minimal stack under
[`Brontide.Minimal.Binding/Portable/`](../../Minimal/src/Brontide.Minimal.Binding/Portable/), using
Minimal-owned algebraic types and explicit results rather than a translation of the Reference
surface. Each stack passes the neutral vectors in a fixed direct-call and a negotiated process
realization whose category-level observations match, and Cooling and Catalog are fixtures over the
reusable layer rather than definitions of it.
[`build/verify-portable-binding.ps1`](../../build/verify-portable-binding.ps1) validates the neutral
layer without loading either stack and then runs both stacks' evidence against it.

**PB4 is the next item**: the direct-versus-process parity phase, which normalizes only
category-level portable observations across the two realizations in each stack and retains
implementation-specific diagnostic codes as non-normative data. The three migration obligations PB1
recorded — deterministic map key ordering, schema-guided values, and frameless denial — are now
discharged on both sides; they are described in
[`binding/portable/README.md`](../../binding/portable/README.md). The two stacks have still never
spoken to each other over the portable contract: that pairing is PB5.

The former Priority 0 documentation relocation is complete; its archived plan is the
[Pinned Documentation Relocation Plan 0.1](../archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md).
No documentation prerequisite now precedes planned implementation work.

## Other planned areas

| Area | Planning source | Current implementation state |
| --- | --- | --- |
| Architecture 0.8 | [`Brontide-Architecture-0.8.md`](./architecture/Brontide-Architecture-0.8.md) | Complete draft; implementation evidence pending; not ratified. |
| Channel | [`Channel Design Note`](./channel/Brontide-Design-Note-Channel-0.1.md), [`Draft Channel Contract`](./channel/Brontide-Draft-Channel-Contract-0.1.md), and [requirements ledger](./channel/architecture-0.8-channel-requirements-and-risk-ledger.md) | Cooling/Catalog evidence exists; reusable Channel realization remains planned. |
| Component Management | [design note](./component-management/Brontide-Design-Note-Component-Management-0.1.md) and [`implementation plan`](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md) | CM0 fixtures are implemented; CM1–CM6 remain planned. |
| Composition | [`Composition Design Note`](./composition/Brontide-Design-Note-Composition-0.1.md) and [Composition Without a Kernel](./architecture/Brontide-Architecture-Composition-Without-a-Kernel.md) | Experimental composition evidence exists; the proposed architecture is not ratified. |
| Enrichment | [`Enrichment Design Note`](./enrichment/Brontide-Design-Note-Enrichment-0.1.md) | Targeted experimental evidence exists; the wider design remains work in progress. |
| Persistent Information | [`Persistent Information Design Note`](./persistent-information/Brontide-Design-Note-Persistent-Information-0.1.md) | Design direction only. |
| Topology and Guardians | [`Topology Design Note`](./topology/Brontide-Design-Note-Topology-0.1.md) | Recorded design direction; not ratified. |
| Reference 0.3 plan | [`Reference implementation plan`](../../Reference/docs/future/Brontide-Reference-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |
| Minimal 0.3 plan | [`Minimal implementation plan`](../../Minimal/docs/future/Brontide-Minimal-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |

Planned documents must state what is already implemented separately from what remains. When a plan
is completed, move it to `docs/archive/<area>/` and move lasting operational guidance or evidence to
`docs/current/` or the owning implementation.
