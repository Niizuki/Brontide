# Brontide Reference Stack experimental and sideline projects

This registry separates exploratory work from Brontide Reference Stack milestones and normative Brontide conformance.
A sideline project may provide useful evidence without becoming a required part of the main
showcase or implying that Brontide has ratified its contracts.

Related Architecture 0.7 implementation notes are in
[`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). Each entry below names its own
design context and remains
experimental even when they are required to produce evidence for the Architecture 0.7 Complete
Draft.

| Project | Classification | Status | Evidence boundary |
| --- | --- | --- | --- |
| Architecture 0.7 Composition delta | Experimental architecture evidence | C1 selection tested in R1; static binding planned in R3 | Composite Constraint selection and static Attribute-constrained binding remain outside Base. Accepted evidence may support the 0.7 draft without ratifying Component, Attribute, or Binding Plan vocabularies. |
| `Brontide.Reference.Experimental.PersistentInformation` | Experimental architecture evidence | Planned in R4; project not yet created | The first Opaque Corpus/Dataset/Store-role slice may test C4/C5 only. It must not imply a complete persistence system, deep Router policy, or ratified persistent-information extension. |
| `Brontide.Reference.Experimental.ComponentManagement` | Experimental architecture evidence (fake harness) | CM0 vocabulary and fixture loader implemented and tested | Fake, deterministic Component Manager for the [Component Management Implementation Plan 0.1](../../docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md). CM0 delivers native identity spaces and a strict, fail-closed loader for the shared `component-management/` fixtures. It is not a real marketplace, package manager, loader, or security product, and is not an Architecture 0.8 conformance claim. Later phases (CM1-CM6) remain planned. |
| GPU execution | Experimental sideline | Planned | Execute the same semantic image Operation through an explicitly eligible GPU provider while exposing compilation, buffers, host/device copies, batching, dispatch, failure domain, and CPU fallback. It must not infer GPU compatibility from ordinary Operation conformance and must not be represented by the existing `System.Numerics` vector provider. |

Graduation into the main showcase would require repeatable GPU execution tests, structured
operational observations, honest fallback behavior, and evidence that the transformation module
does not need an application-level redesign.
