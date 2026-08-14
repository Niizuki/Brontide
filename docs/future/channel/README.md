# Channel future-work index

Channel 0.1 is retained, implemented experimental evidence. It is not ratified and its provisional
logical vocabulary is not the stable successor contract.

Channel 0.2 is the current Priority 1 redesign. Its four first-batch owner rulings were resolved on
2026-08-11. B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1-U8, V1-V3, W1-W6, X1-X7, Y1-Y4, and
Z1-Z4 all have contract-first corrections, including a closed state/event grid. AA1-AA3 are corrected
in this index and the future-work index, which had fallen behind every family since V2, and AB1-AB2 in
the redesign plan and the responsibility matrix. The V through
AA families were raised by author-side iteration passes over each other's corrections rather than by
an independent review, each by asking what the previous fix depended on; that method has not yet
stopped producing findings. Every recorded finding being closed is not a verdict; implementation remains gated
on a conforming fresh independent closure re-review, which is the only judgement that can close the
batch.

## Channel 0.2 design foundation

Every artifact below awaits the same cycle: one fresh independent closure re-review, now of the
correction sequence that runs from S1 through Z4. R1 was a disagreement between C8 and the recipient state/event grid about a cancellation
that races recipient admission; the 2026-08-13 owner ruling holds the control until admission
resolves rather than faulting it. S1 was that the same correction kept the neighbouring `unseen` cell
sound only by asserting, in the grid alone, a delivery-ordering guarantee C4 and C11 disclaimed and
the responsibility matrix assigned to `delivery-facet`. The second 2026-08-13 ruling gives that fact
an owner: Channel 0.2 core promises intra-interaction frame order, narrowly scoped, and everything
that depends on it now says so. What followed is the part worth knowing before reading any artifact
below: U1 found that the property carrying that promise could not fail, and every family since has
been a layer under the previous correction rather than a new subject — the property's subject not
compared, its mutation not executable, its language unable to express it, its witness abolished by a
neighbouring fix, and the evidence it needs missing from the inventory Batch 2 works from. See the
[review handoff](./reviews/README.md#exact-next-work) for the live path.

| Artifact | Purpose | Current state |
| --- | --- | --- |
| [Redesign and migration plan](./Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md) | Programme boundary, batches, migration policy, and completion gate | Active plan |
| [C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md) | Observable capability, effects, failures, properties, evidence, and silence | N2/F1/F2/D1-D4/T3/R1/S1/S2/U1/W2-W4/X1/X5/Y1/Y2/Z3 corrected; C4 owns intra-interaction frame order with `C4-P2`, stated over the refusal a reordering produces |
| [Session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md) | Exact Channel-owned session states and transitions | D1 corrected; unchanged by the sixth, seventh, and eighth reviews and by the U-Z correction passes |
| [Interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md) | Admission, concurrency, cancellation, terminality, and effect certainty | B1/B2/N2/F1/F2/D2-D4/T3/R1/R2/S2/W4/X1/X3/X5/Y3 corrected; `validating` carries loss and drain rows, and an identity refused at `unseen` retains nothing and owns no latch |
| [State/event coverage](./Brontide-Channel-0.2-State-Event-Coverage-0.1.md) | Closed-world coverage of every session, initiator, recipient, and terminal event family | Added for D1-D4; T3/R1/R3/S1/S2/U8/W4/X1/X2/X5/Z2 corrected; 108 cells enumerated independently, none empty; carries the ordering fact C4 owns |
| [Responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md) | One semantic owner and neutral crossing artifact per concern | B3/N1/S1/U2 corrected; `Intra-interaction frame order` added, owned by `channel`, and the owner-identifier vocabulary is now closed |
| [Contract-completeness review](./Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md) | Separate review of silence and extension pressure | All findings through T1-T4, R1-R3, S1-S3, U1-U8, and the V-Z iteration families corrected and dispositioned |
| [0.1-to-0.2 migration ledger](./Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md) | Disposition of predecessor Shapes, fields, states, categories, limits, observations, vectors, and goldens | B4/N1/N3/F3/D5/T1/T2/S1/Z4 corrected; the ordering non-promise is **replaced**, and the new-evidence inventory carries the ordering mutations |
| [Neutral contract/vector brief](./Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md) | Batch 2 data-only artifact, identity, property, vector, observation, and golden boundaries | Author pass plus U3/V1/V2/W1/W2/W5/W6/X1/X2/X4/Y1/Y4/Z1 corrections; property operators, vector format, parity profile, and provider boundary now carry what `C4-P2` needs |
| [Design reviews](./reviews/README.md) | Fresh-context review policy and retained attestations | 8 negative attestations retained, the seventh the first with complete isolation, plus 2 iteration reviews recording the author-side V-Z passes |

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
