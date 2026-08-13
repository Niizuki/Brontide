# Channel future-work index

Channel 0.1 is retained, implemented experimental evidence. It is not ratified and its provisional
logical vocabulary is not the stable successor contract.

Channel 0.2 is the current Priority 1 redesign. Its four first-batch owner rulings were resolved on
2026-08-11. B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R2, and R3 all have contract-first corrections
confirmed closed by the seventh review, including a closed state/event grid. R1's correction closed
the `validating` cell and not the `unseen` one, which is blocking finding **S1**. Implementation
remains gated on a conforming fresh independent closure re-review of an S1 correction that does not
yet exist.

## Channel 0.2 design foundation

Every artifact below awaits the same cycle: one fresh independent closure re-review, now of an S1
correction. R1 was a disagreement between C8 and the recipient state/event grid about a cancellation
that races recipient admission; the 2026-08-13 owner ruling holds the control until admission
resolves rather than faulting it. S1 is that the same correction keeps the neighbouring `unseen` cell
sound only by asserting, in the grid alone, a delivery-ordering guarantee C4 and C11 disclaim and the
responsibility matrix assigns to `delivery-facet`. It is **blocked on an owner ruling**. See the
[review handoff](./reviews/README.md#exact-next-work) for the live path and the option set.

| Artifact | Purpose | Current state |
| --- | --- | --- |
| [Redesign and migration plan](./Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md) | Programme boundary, batches, migration policy, and completion gate | Active plan |
| [C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md) | Observable capability, effects, failures, properties, evidence, and silence | N2/F1/F2/D1-D4/T3/R1 corrected; C4 and C8 carry blocking S1 |
| [Session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md) | Exact Channel-owned session states and transitions | D1 corrected; unchanged by the sixth and seventh reviews |
| [Interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md) | Admission, concurrency, cancellation, terminality, and effect certainty | B1/B2/N2/F1/F2/D2-D4/T3/R1/R2 corrected; nonblocking S2 open |
| [State/event coverage](./Brontide-Channel-0.2-State-Event-Coverage-0.1.md) | Closed-world coverage of every session, initiator, recipient, and terminal event family | Added for D1-D4; T3/R1/R3 corrected; 108 cells enumerated independently, none empty; carries the unowned ordering sentence of blocking S1 |
| [Responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md) | One semantic owner and neutral crossing artifact per concern | B3/N1 corrected; unchanged by the fifth, sixth, and seventh reviews; its ordering row is the evidence for blocking S1 |
| [Contract-completeness review](./Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md) | Separate review of silence and extension pressure | All findings through T1-T4 and R1-R3 corrected; S1 and S2 absent from the silence inventory |
| [0.1-to-0.2 migration ledger](./Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md) | Disposition of predecessor Shapes, fields, states, categories, limits, observations, vectors, and goldens | B4/N1/N3/F3/D5/T1/T2 corrected |
| [Neutral contract/vector brief](./Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md) | Batch 2 data-only artifact, identity, property, vector, observation, and golden boundaries | Author pass drafted |
| [Design reviews](./reviews/README.md) | Fresh-context review policy and retained attestations | Seven negative reviews retained; the seventh is the first with complete isolation |

No Channel 0.2 schema, public type, package, host, provider, or encoding is authorized while a fresh
independent closure re-review is pending or a blocking independent-review finding remains open.

## Retained Channel 0.1 evidence

- [Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md)
- [Draft Channel Contract 0.1](./Brontide-Draft-Channel-Contract-0.1.md)
- [Architecture 0.8 Channel requirements and risk ledger](./architecture-0.8-channel-requirements-and-risk-ledger.md)
- [`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json)
- [Portable Binding 0.1 neutral artifacts](../../../binding/portable/README.md)

The migration programme does not rewrite or relocate this evidence. Channel 0.2 references it as
predecessor provenance and authors new versioned artifacts in later batches.
