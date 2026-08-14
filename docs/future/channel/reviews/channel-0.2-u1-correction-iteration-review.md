# Channel 0.2 U1 correction iteration review

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-8-2026-08-14-3b27e3a`

Reviewed work: the U1 correction, `fix(channel): make C4-P2 falsifiable`

Date: 2026-08-14

**This is an iteration review, not an attestation.** Under
[Two kinds of review](./README.md#two-kinds-of-review) it is author-side work: it shares an actor and
a context with the correction it examines and with closure review 8 that raised U1, and it corrected
what it found in the same pass. It **does not close the first batch, does not authorize Batch 2, does
not produce the closure record, and its verdict is not the conforming verdict the Closure section
requires**. Its purpose is to spend cheap context on defects so a fresh reviewer does not spend its
one shot of cold context on them.

Status of the work after this pass: **ready to be reviewed, not reviewed.**

## Verdict

The U1 correction holds as a statement of the property. It did not hold as *evidence*: two artifacts
had to change before `C4-P2` could actually be evaluated or its mutation executed, and neither
changed in the correction commit. Both are corrected here as **V1** and **V2**. A third observation,
**V3**, is recorded without correction because it belongs to an owner decision.

## Findings

### V1 — the property quantified over a fact the parity profile does not compare — corrected

`C4-P2`'s first conjunct turns on a recipient `rejected-protocol` **caused by a cancellation control
naming an unopened identity**. The neutral brief's normative parity list compared "terminal provenance
and peer-fault/local-loss **category** where present". The category is
`invalid-interaction-correlation`, which the migration ledger says covers "missing, extra,
wrong-session, reused, or mismatched identities as the detailed reason" — and that detailed reason was
normative nowhere.

So the property distinguished a refusal the evidence set could not distinguish. Two realizations could
disagree about *why* an interaction was refused, agree on everything the parity profile compares, and
`C4-P2` would be evaluating a fact no vector could pin down. That is a weaker version of the same
defect U1 named: a property whose subject is not observable.

**Corrected** by adding the peer-fault detailed reason to the brief's normative comparison wherever
its category declares a closed set, with the `C4-P2` case named as the reason.

### V2 — nothing was permitted to execute the named mutation — corrected

The neutral provider boundary authorised the endpoint to support "deterministic fault/loss injection
named by vectors". `C4-control-precedes-request` is neither a fault nor a loss: it requires an
endpoint to deliver two frames of one interaction in an order the sender did not commit them in. No
artifact in the first batch authorised any endpoint to do that, and a conforming realization by
definition cannot.

`C4-P2` would therefore have carried a named mutation that nothing in the evidence set was permitted
to produce. A property whose mutation cannot be run is unfalsifiable in practice however well it is
worded — which is U1 again, one layer down, at the evidence boundary rather than the property
boundary. This is the finding that most justifies the iteration pass: it is invisible from the
contract alone and only appears when the question is *who actually runs this*.

**Corrected** by authorising deterministic per-interaction reordering injection in the neutral
provider, explicitly bounded: it exists only to execute a declared mutation, is never a legal delivery
mode, no conforming realization may offer it, and a vector that does not name it receives commit
order.

### V3 — the two corrections are U3's first instalment, and U3 is still open — not corrected

Closure review 8 raised **U3**: the neutral brief was the only first-batch artifact carrying no trace
of the S1 correction. V1 and V2 are both brief changes, which is U3 being paid down one forced
instalment at a time rather than dispositioned. Still absent from the brief: the realization's
per-interaction frame order declaration in the establishment rule and the `established-profile.json`
boundary, and any vector group owning the ordering mutation.

Not corrected here, because deciding how much of the S1/U1 obligation the brief must carry before
Batch 2 is an owner call about Batch 2's scope, not a defect with one right answer. **U2, U3, U4, U7,
and U8 all remain open.** U5 closed as part of U1's correction and U6 closed when the pin clause was
rewritten.

## What was re-verified, and what that is worth

`C4-P2` as corrected was evaluated by an evaluator written from the published prose alone, importing
no repository code: green on eight known-good cases and red on both reordering mutations. Restricting
each conjunct to one endpoint's own frames was found necessary by that probe rather than by reading —
without it, C8's legal late control after a peer terminal and a duplicate terminal from a
nonconformant peer both fail the property.

**That evidence is worth less than it looks, and the reason is the point of the closure requirement.**
The evaluator was written by the same actor that wrote the property, from the same reading of the same
prose. It can only refute the property where the author's model of the design is wrong in a way the
author also encoded into the evaluator's inputs — it cannot refute the model itself. It caught the
same-endpoint restriction because that was a case the author had not considered; it would not catch a
shared misreading. A fresh reviewer writing its own evaluator from the same prose is a genuinely
different experiment.

The failing-check-first discipline was followed for V1 and V2 as it was for U1: both checks were
written before the corrections, observed failing against the pre-correction brief, and the design gate
returns to green after.

## Gates

Run after the V1/V2 corrections:

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | passes |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | exactly one message, on `C12-P1` |
| `build/verify-doc-links.ps1` | passes |
| `build/verify-text.ps1` | passes |
| `build/verify-interchange.ps1` | exit 0; only the two pre-existing `Cbi51` restart-policy skips |

Gates passing is not a verdict. Every prior cycle's blocking finding was raised against artifacts
whose gates were green, because a gate can only ask questions someone already thought to encode.

## What the next closure review should not inherit from this

This document is retained so a fresh reviewer can see what was already examined and stop re-deriving
it. It is not evidence that any of it is right. In particular the reviewer should not accept from here
that `C4-P2` is falsifiable, that the same-endpoint restriction is sufficient, that reordering
injection is correctly bounded, or that V1 makes the property's subject observable — all four are this
author's conclusions about this author's corrections, and the sharpest questions in
[Exact next work](./README.md#exact-next-work) are unchanged by anything recorded here.
