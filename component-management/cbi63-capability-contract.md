# CBI63 capability contract — governed interruption reconciliation

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI49 translates host evidence about an interrupted cadence cycle into CBI48's retry or abandonment.
Its evidence carries one verdict for the whole cycle, which was right when a cycle was one loop.
CBI61 made a cycle govern two, and CBI62 established that a marker written *after* an effect is worse
than no marker at all. CBI63 reconciles a governed interruption.

The item that scheduled it asked for evidence naming which of the two loops the host had verified.
That is not what it needs. Two of the three things a governed cycle can do are recorded durably by
the components that do them, so the host should not be asserting anything about them; the third is
the one CBI48 says no local journal can commit atomically with its own cursor. The evidence is
therefore *narrowed* rather than widened, and the narrowing is the capability.

This is not a clock, a network monitor, a daemon, a cross-process owner, a secure evidence custodian,
or a proof that external effects are absent.

## Capabilities

### C1 — the cursor is recorded with the in-flight marker, in the same write

Before a governed cycle runs, the journal records the durable cursor it is about to act on — the
authority generation and active authority, and the policy sequence and policy identity — in the same
atomic write that already marks the attempt in-flight.

This is the exact device CBI62 refused, inverted, and the inversion is the whole reason it is sound.
CBI62 refused a marker written *after* the rotation returns, because such a write is not atomic with
the effect it describes and so opens a second indeterminate window. A cursor written *before* the
cycle describes state that already exists, rides in a write the protocol already performs, and opens
no window at all.

Property: recording the cursor adds no journal write. A governed run performs exactly the transitions
an ungoverned one does.

### C2 — a governed interruption is not reconcilable by the ungoverned path

CBI49's evidence carries one verdict for everything the cycle did. Applied to a governed
interruption, that verdict speaks for effects the host cannot have inspected and need not have. An
in-flight journal that recorded a cursor is refused by CBI49's path as
`cadence-reconciliation-governed`, and no journal state changes.

Property: no journal carrying a recorded cursor reaches a CBI48 transition through CBI49's evidence.

### C3 — the evidence cannot speak about what verifies itself

Governed evidence names the run, attempted index, and attempted instant exactly as CBI49's does, and
carries exactly one verdict: the serving one. There is no field for the rotation or the policy
update, so a host cannot assert `no-effects-confirmed` about an effect it did not look at. The absent
fields are the contract, as they were for CBI17's succession and CBI18's declaration.

Property: no governed vector reaches a transition on the strength of an assertion about the authority
generation or the policy sequence.

### C4 — the two durable effects are derived, and disagreement with the guard is refused

The reconciler compares the recorded cursor with what the registry holds now. A generation or
sequence that advanced is a derived effect; one that is unchanged is a derived absence. Both are
reported next to the decision. A generation or sequence *below* the recorded cursor is a rollback the
floors exist to prevent, so it is refused as `governed-reconciliation-cursor-regressed` and changes
nothing.

Property: every governed result reports a derived rotation and policy observation, and neither is
ever taken from the evidence.

### C5 — a missing cursor is reported, never guessed

A journal written before this slice has no cursor. Its interruption is refused as
`governed-reconciliation-cursor-absent` rather than reconciled against a baseline the reconciler
would have to invent. The host's route forward is CBI49's ungoverned path, which is correct for the
ungoverned run that journal describes.

Property: no derivation is performed against an absent cursor.

### C6 — the serving verdict alone selects CBI48's transition

`no-effects-confirmed` selects retry, `effects-accounted-for` selects abandonment, and `unknown`
defers and leaves the in-flight marker exactly as it was. CBI48's interruption and retry counting is
unchanged, and a derived rotation or policy effect does not block retry — CBI62 established that a
retried governed cycle cannot double-apply either half, so the derived facts are reported for the
host rather than used as a veto.

Property: one accepted governed reconciliation produces exactly one durable CBI48 transition, and the
counts it produces are the ones CBI49 produces for the same verdict.

### C7 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the reconciliation
code, the derived rotation and policy observations, the journal phase, and the interruption and retry
counts.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

CBI63 derives what the local durable record already states. It does not inspect providers, prove a
sweep's retirements or cleanup happened, or manufacture the serving verdict — that remains the host's
observation and the one thing nothing here can check. A derived rotation whose floor was not retained
is reported rather than repaired: custody repair is the host's, and refusing retry until it happened
would be a policy this slice has no grounds to invent. Cross-process ownership and privileged custody
of either floor remain separate.
