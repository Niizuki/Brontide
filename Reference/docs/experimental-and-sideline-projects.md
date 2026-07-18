# Brontide Reference Stack experimental and sideline projects

This registry separates exploratory work from Brontide Reference Stack milestones and normative Brontide conformance.
A sideline project may provide useful evidence without becoming a required part of the main
showcase or implying that Brontide has ratified its contracts.

Current architectural planning is routed through
[`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). The entries below remain
experimental even when they are required to produce evidence for the Architecture 0.7 Complete
Draft.

| Project | Classification | Status | Evidence boundary |
| --- | --- | --- | --- |
| Architecture 0.7 Composition delta | Experimental architecture evidence | Planned in R1 and R3 | Composite Constraint selection and static Attribute-constrained binding remain outside Base. Accepted evidence may support the 0.7 draft without ratifying Component, Attribute, or Binding Plan vocabularies. |
| `Brontide.Reference.Experimental.PersistentInformation` | Experimental architecture evidence | Planned in R4; project not yet created | The first Opaque Corpus/Dataset/Store-role slice may test C4/C5 only. It must not imply a complete persistence system, deep Router policy, or ratified persistent-information extension. |
| GPU execution | Experimental sideline | Planned | Execute the same semantic image Operation through an explicitly eligible GPU provider while exposing compilation, buffers, host/device copies, batching, dispatch, failure domain, and CPU fallback. It must not infer GPU compatibility from ordinary Operation conformance and must not be represented by the existing `System.Numerics` vector provider. |

Graduation into the main showcase would require repeatable GPU execution tests, structured
operational observations, honest fallback behavior, and evidence that the transformation module
does not need an application-level redesign.
