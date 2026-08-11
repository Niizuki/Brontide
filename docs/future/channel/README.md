# Channel future-work index

Channel 0.1 is retained, implemented experimental evidence. It is not ratified and its provisional
logical vocabulary is not the stable successor contract.

Channel 0.2 is the current Priority 1 redesign. Its four first-batch owner rulings were resolved on
2026-08-11. B1-B4 and N1-N3 have contract-first corrections; implementation remains gated on a
conforming fresh final closure review.

## Channel 0.2 design foundation

| Artifact | Purpose | Current state |
| --- | --- | --- |
| [Redesign and migration plan](./Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md) | Programme boundary, batches, migration policy, and completion gate | Active plan |
| [C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md) | Observable capability, effects, failures, properties, evidence, and silence | N2 corrected; final closure review pending |
| [Session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md) | Exact Channel-owned session states and transitions | Author pass drafted |
| [Interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md) | Admission, concurrency, cancellation, terminality, and effect certainty | B1/B2/N2 corrected; final closure review pending |
| [Responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md) | One semantic owner and neutral crossing artifact per concern | B3/N1 corrected; final closure review pending |
| [Contract-completeness review](./Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md) | Separate review of silence and extension pressure | B1-B4/N1-N3 correction passes complete; final review pending |
| [0.1-to-0.2 migration ledger](./Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md) | Disposition of predecessor Shapes, fields, states, categories, limits, observations, vectors, and goldens | B4/N1/N3 corrected; final closure review pending |
| [Neutral contract/vector brief](./Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md) | Batch 2 data-only artifact, identity, property, vector, observation, and golden boundaries | Author pass drafted |
| [Design reviews](./reviews/README.md) | Fresh-context review policy and retained attestations | Two negative reviews retained; final closure review pending |

No Channel 0.2 schema, public type, package, host, provider, or encoding is authorized while the
independent review is pending or a blocking independent-review finding remains open.

## Retained Channel 0.1 evidence

- [Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md)
- [Draft Channel Contract 0.1](./Brontide-Draft-Channel-Contract-0.1.md)
- [Architecture 0.8 Channel requirements and risk ledger](./architecture-0.8-channel-requirements-and-risk-ledger.md)
- [`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json)
- [Portable Binding 0.1 neutral artifacts](../../../binding/portable/README.md)

The migration programme does not rewrite or relocate this evidence. Channel 0.2 references it as
predecessor provenance and authors new versioned artifacts in later batches.
